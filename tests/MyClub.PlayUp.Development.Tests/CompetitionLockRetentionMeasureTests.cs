// -----------------------------------------------------------------------
// <copyright file="CompetitionLockRetentionMeasureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// A10-R7 lot 1 — measure competition advisory lock wait and retention (p50/p95/max) on exclusive writes.
/// Uses dedicated <see cref="LockMeasurePostgresFixture"/> (interceptors isolated from <see cref="DevelopmentPostgresFixture"/>).
/// </summary>
[Collection("LockMeasurePostgres")]
[Trait("Category", "Integration")]
[Trait("Category", "PerformanceBaseline")]
public sealed class CompetitionLockRetentionMeasureTests(
    LockMeasurePostgresFixture fixture,
    ITestOutputHelper output)
{
    private static readonly HashSet<string> TemplateSeedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "ligue-1",
        "champions-league",
        "world-cup",
        "coupe-de-france"
    };

    private static readonly (string Seed, string Jeu, string TeamsHint)[] BigGameSeeds =
    [
        ("ligue-1:prepared", "ligue-1", "18 équipes"),
        ("champions-league:prepared", "champions-league", "32 équipes · groupes 8×4"),
        ("swiss-8x3:prepared", "swiss-8x3", "8 équipes · 3 tours"),
        ("world-cup", "world-cup", "32 équipes · groupes + KO (seed terminé)"),
        ("coupe-de-france", "coupe-de-france", "32 équipes · coupe terminée")
    ];

    private static string FormatJeu((string Seed, string Jeu, string TeamsHint) entry) =>
        $"{entry.Jeu} ({entry.TeamsHint})";

    [Fact]
    public async Task Measure_competition_lock_retention_campaignAsync()
    {
        var sampleCount = CompetitionLockRetentionMeasureSupport.ResolveSampleCount();
        var ciReduced = CompetitionLockRetentionMeasureSupport.IsCiReducedCampaign(sampleCount);
        output.WriteLine($"PLAYUP_LOCK_MEASURE_SAMPLES={sampleCount}");
        output.WriteLine($"CI_REDUCED_CAMPAIGN={ciReduced}");

        var rows = new List<MeasureRow>();
        var skipped = new List<SkippedMeasure>();

        await Assert_contended_wait_is_positive_when_lock_heldAsync();

        await MeasureApplyProgressionCupSfAsync(sampleCount, rows, skipped);
        await MeasureDematerializeBigGamesAsync(sampleCount, ciReduced, rows, skipped);
        await MeasureMaterializeBigGamesAsync(sampleCount, ciReduced, rows, skipped);
        if (ciReduced || sampleCount >= CompetitionLockRetentionMeasureSupport.FullCampaignSampleThreshold)
        {
            await MeasureMaterializeRecetteG1Async(sampleCount, rows, skipped);
        }

        await TryMeasureConfirmOperationsAsync(skipped);

        var recordGoalSqlBaseline = await MeasureRecordGoalSqlBaselineAsync();
        var concurrent = await MeasureConcurrentRecordGoalAsync(sampleCount);

        WriteMeasurementTables(rows);
        WriteSkippedOperations(skipped);
        WriteConcurrentBlock(concurrent);
        output.WriteLine($"RecordGoal SQL baseline (lot 1 hors verrou) : {CompetitionLockRetentionMeasureSupport.RecordGoalSqlBaselineWithoutLock}");
        output.WriteLine($"RecordGoal SQL mesuré (verrou réel) : {recordGoalSqlBaseline} commandes EF");

        recordGoalSqlBaseline.Should().BeLessThanOrEqualTo(
            CompetitionLockRetentionMeasureSupport.RecordGoalSqlCeiling,
            "RecordGoal plafond = baseline lot 1 + overhead verrou");

        rows.Should().NotBeEmpty("at least one exclusive operation must be measured");
        concurrent.Attempts.Should().BeGreaterThan(0);
        concurrent.InvalidOperationExceptions.Should().Be(0);
        concurrent.UnexpectedFailures.Should().BeEmpty();
        concurrent.LockWaits.Count.Should().Be(concurrent.Attempts);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Test-only session lock; key from CompetitionAdvisoryLock.Key.")]
    private async Task Assert_contended_wait_is_positive_when_lock_heldAsync()
    {
        await ReloadSeedAsync("cup-sf-running").ConfigureAwait(false);
        CompetitionId competitionId;
        using (var scope = fixture.Services.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            competitionId = (await competitions.ListAsync().ConfigureAwait(false)).Single().Id;
        }

        var key = CompetitionAdvisoryLock.Key(competitionId);
        var holdDuration = TimeSpan.FromMilliseconds(350);
        var holdReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holdTask = Task.Run(async () =>
        {
            await using var connection = new Npgsql.NpgsqlConnection(fixture.PlayUpConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var hold = new Npgsql.NpgsqlCommand(
                $"SELECT pg_advisory_lock({key})",
                connection);
            await hold.ExecuteNonQueryAsync().ConfigureAwait(false);
            holdReady.SetResult();
            await Task.Delay(holdDuration).ConfigureAwait(false);
            await using var release = new Npgsql.NpgsqlCommand(
                $"SELECT pg_advisory_unlock({key})",
                connection);
            await release.ExecuteNonQueryAsync().ConfigureAwait(false);
        });

        await holdReady.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await Task.Delay(30).ConfigureAwait(false);

        using var measureScope = fixture.Services.CreateScope();
        var snapshot = await CompetitionLockRetentionMeasureSupport.MeasureExclusiveOperationAsync(
            measureScope,
            async scope =>
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.ExecuteExclusiveAsync(
                    competitionId,
                    static _ => Task.CompletedTask).ConfigureAwait(false);
            }).ConfigureAwait(false);

        snapshot.LockWait.Should().BeGreaterThan(TimeSpan.Zero, "session lock must force xact lock wait");
        snapshot.LockRetention.Should().BeGreaterThan(TimeSpan.Zero);

        await holdTask.ConfigureAwait(false);
    }

    private async Task MeasureApplyProgressionCupSfAsync(
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        const string seed = "cup-sf-running";
        const string jeu = "cup-sf-running (8 équipes · QF→SF Running)";
        try
        {
            var snapshots = new List<CompetitionAdvisoryLockTimingSnapshot>(sampleCount);
            CompetitionShape? shape = null;
            for (var index = 0; index < sampleCount; index++)
            {
                await ReloadSeedAsync(seed).ConfigureAwait(false);
                var progression = await ResolveCupSfProgressionTargetAsync().ConfigureAwait(false);
                shape = await LoadShapeForCupSfAsync().ConfigureAwait(false);
                using var scope = fixture.Services.CreateScope();
                snapshots.Add(await CompetitionLockRetentionMeasureSupport.MeasureExclusiveOperationAsync(
                    scope,
                    async inner =>
                    {
                        var executor = inner.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                        await executor.ApplyProgressionOutcomeAsync(
                            progression.SourceStageId,
                            progression.FixtureId).ConfigureAwait(false);
                    }).ConfigureAwait(false));
            }

            shape.Should().NotBeNull();
            rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                "ApplyProgressionOutcomeAsync",
                jeu,
                shape!.Value,
                snapshots));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            skipped.Add(new SkippedMeasure("ApplyProgressionOutcomeAsync", jeu, ex.Message));
        }
    }

    private async Task MeasureDematerializeBigGamesAsync(
        int sampleCount,
        bool ciReduced,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        foreach (var entry in BigGameSeeds)
        {
            if (ciReduced && entry.Seed is not ("swiss-8x3:prepared" or "ligue-1:prepared"))
            {
                output.WriteLine($"CI skip dematerialize · {entry.Jeu}");
                continue;
            }

            await MeasureDematerializeOneAsync(entry.Seed, entry.Jeu, entry.TeamsHint, sampleCount, rows, skipped)
                .ConfigureAwait(false);
        }
    }

    private async Task MeasureDematerializeOneAsync(
        string seed,
        string jeuLabel,
        string teamsHint,
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        var jeu = $"{jeuLabel} ({teamsHint})";
        if (seed is "ligue-1:prepared" or "champions-league:prepared")
        {
            skipped.Add(new SkippedMeasure(
                "DematerializePhaseAsync",
                jeu,
                "template :prepared → compétition Running : DematerializationCapability refuse FullPhase (StageLifecycleNotSupported)."));
            return;
        }

        try
        {
            var snapshots = new List<CompetitionAdvisoryLockTimingSnapshot>(sampleCount);
            CompetitionShape? shape = null;
            for (var index = 0; index < sampleCount; index++)
            {
                await ReloadSeedAsync(seed).ConfigureAwait(false);
                var stageId = await PrepareForDematerializeAsync(seed).ConfigureAwait(false);
                shape = await LoadPrimaryShapeAsync().ConfigureAwait(false);
                using var scope = fixture.Services.CreateScope();
                snapshots.Add(await CompetitionLockRetentionMeasureSupport.MeasureExclusiveOperationAsync(
                    scope,
                    async inner =>
                    {
                        var executor = inner.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                        await executor.DematerializePhaseAsync(stageId).ConfigureAwait(false);
                    }).ConfigureAwait(false));
            }

            rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                "DematerializePhaseAsync",
                jeu,
                shape ?? throw new InvalidOperationException("shape manquant"),
                snapshots));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            skipped.Add(new SkippedMeasure("DematerializePhaseAsync", jeu, ex.Message));
        }
    }

    private async Task MeasureMaterializeBigGamesAsync(
        int sampleCount,
        bool ciReduced,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        foreach (var entry in BigGameSeeds)
        {
            if (entry.Seed.StartsWith("swiss-8x3", StringComparison.Ordinal))
            {
                skipped.Add(new SkippedMeasure(
                    "MaterializeMatchesAsync",
                    FormatJeu(entry),
                    "format Suisse : production via GenerateNextRound, MaterializeMatches non applicable (§ domaine)."));
                continue;
            }

            if (entry.Seed.StartsWith("coupe-de-france", StringComparison.Ordinal))
            {
                skipped.Add(new SkippedMeasure(
                    "MaterializeMatchesAsync",
                    FormatJeu(entry),
                    "format Coupe : matérialisation via MaterializeCupFromOccupiedSlots, pas MaterializeMatches."));
                continue;
            }

            if (entry.Seed == "world-cup")
            {
                skipped.Add(new SkippedMeasure(
                    "MaterializeMatchesAsync",
                    FormatJeu(entry),
                    "seed world-cup terminé (Completed) — pas de phase Ready/Championship ou Groups rematérialisable sans reconstruction complète du seed."));
                continue;
            }

            if (ciReduced && entry.Seed is not "ligue-1:prepared")
            {
                output.WriteLine($"CI skip materialize · {FormatJeu(entry)}");
                continue;
            }

            await MeasureMaterializeChampionshipOrGroupsAsync(
                entry.Seed,
                FormatJeu(entry),
                sampleCount,
                rows,
                skipped).ConfigureAwait(false);
        }
    }

    private async Task MeasureMaterializeRecetteG1Async(
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        await MeasureMaterializeChampionshipOrGroupsAsync(
            "structure-materialize-recette",
            "structure-materialize-recette (RECETTE G1 · 6/6 groups)",
            sampleCount,
            rows,
            skipped).ConfigureAwait(false);
    }

    private async Task MeasureMaterializeChampionshipOrGroupsAsync(
        string seed,
        string jeu,
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        if (seed is "ligue-1:prepared" or "champions-league:prepared")
        {
            skipped.Add(new SkippedMeasure(
                "MaterializeMatchesAsync",
                jeu,
                "template :prepared → matchs déjà matérialisés et compétition Running : pas de dematerialize FullPhase pour rejouer MaterializeMatches (même raison lifecycle)."));
            return;
        }

        try
        {
            var snapshots = new List<CompetitionAdvisoryLockTimingSnapshot>(sampleCount);
            CompetitionShape shape = default;
            for (var index = 0; index < sampleCount; index++)
            {
                await ReloadSeedAsync(seed).ConfigureAwait(false);
                using (var shapeScope = fixture.Services.CreateScope())
                {
                    shape = seed == "structure-materialize-recette"
                        ? await CompetitionLockRetentionMeasureSupport.LoadShapeFromScopeAsync(shapeScope)
                            .ConfigureAwait(false)
                        : await CompetitionLockRetentionMeasureSupport.LoadPrimaryTemplateShapeAsync(shapeScope)
                            .ConfigureAwait(false);
                }

                var stageId = shape.PrimaryStageId;
                if (shape.SeedMatchCount > 0 && seed is not "structure-materialize-recette")
                {
                    await StripMatchesForRematerializeAsync(stageId).ConfigureAwait(false);
                }

                using var scope = fixture.Services.CreateScope();
                snapshots.Add(await CompetitionLockRetentionMeasureSupport.MeasureExclusiveOperationAsync(
                    scope,
                    async inner =>
                    {
                        var executor = inner.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                        await executor.MaterializeMatchesAsync(stageId).ConfigureAwait(false);
                    }).ConfigureAwait(false));
            }

            rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                "MaterializeMatchesAsync",
                jeu,
                shape,
                snapshots));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            skipped.Add(new SkippedMeasure("MaterializeMatchesAsync", jeu, ex.Message));
        }
    }

    private async Task TryMeasureConfirmOperationsAsync(List<SkippedMeasure> skipped)
    {
        await ReloadSeedAsync("freshness-actions-recette").ConfigureAwait(false);
        skipped.Add(new SkippedMeasure(
            "ConfirmCascadeReplaceAsync",
            "freshness-actions-recette",
            "Confirm exige un CascadeReplaceTarget + versionToken issu de PreviewCascadeReplaceAsync sur un occupant périmé (slot « Suite ») ; le rejeu consomme l’état et n’est pas idempotent dans ce seed."));

        skipped.Add(new SkippedMeasure(
            "ConfirmResolutionReleaseAsync",
            "resolution-freshness-qual-slot",
            "nécessite un aperçu ReleaseResolution + versionToken valide ; occupants du seed sont déjà « current » (scenario description)."));
        skipped.Add(new SkippedMeasure(
            "ConfirmDefinitiveResolutionReleaseAsync",
            "freshness-actions-recette",
            "nécessite un occupant EnCoursOuIndéterminé avec chemin de libération définitive prêt — états consommés ou non déclenchés de façon rejouable."));
        skipped.Add(new SkippedMeasure(
            "ConfirmDeletedSourceDefinitiveReleaseAsync",
            "—",
            "nécessite une source supprimée en attente de libération définitive — aucun scénario Development ne laisse cet état rejouable idempotemment."));
    }

    private async Task<int> MeasureRecordGoalSqlBaselineAsync()
    {
        await ReloadSeedAsync("champions-league:running").ConfigureAwait(false);
        var live = await PrepareLiveMatchForRecordGoalAsync().ConfigureAwait(false);

        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        counter.Reset();
        await executor.RecordGoalAsync(
            live.MatchId,
            live.ScorerMemberId,
            Side.Home,
            cancellationToken: default).ConfigureAwait(false);
        return counter.CommandCount;
    }

    private async Task<ConcurrentRecordGoalReport> MeasureConcurrentRecordGoalAsync(int sampleCount)
    {
        await ReloadSeedAsync("cup-sf-running").ConfigureAwait(false);
        var targets = await ResolveConcurrentTargetsAsync().ConfigureAwait(false);

        var waits = new List<TimeSpan>(sampleCount);
        var retentions = new List<TimeSpan>(sampleCount);
        var endToEnd = new List<TimeSpan>(sampleCount);
        var lockTimeouts = 0;
        var invalidOps = 0;
        var unexpected = new ConcurrentBag<string>();

        using var stopApply = new CancellationTokenSource();
        var applyLoop = RunContinuousApplyProgressionAsync(targets, unexpected, stopApply.Token);

        await Task.Delay(120).ConfigureAwait(false);

        for (var index = 0; index < sampleCount; index++)
        {
            using var scope = fixture.Services.CreateScope();
            var timing = scope.ServiceProvider.GetRequiredService<CompetitionAdvisoryLockTimingInterceptor>();
            timing.Reset();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                await executor.RecordGoalAsync(
                    targets.RecordGoalMatchId,
                    targets.ScorerMemberId,
                    Side.Home,
                    cancellationToken: default).ConfigureAwait(false);
                var snapshot = timing.RequireLastSnapshot();
                waits.Add(snapshot.LockWait);
                retentions.Add(snapshot.LockRetention);
            }
            catch (ApplicationFailureException ex) when (
                ex.Code == ApplicationErrorCodes.CompetitionLockTimeout)
            {
                lockTimeouts++;
                try
                {
                    var snapshot = timing.RequireLastSnapshot();
                    waits.Add(snapshot.LockWait);
                    retentions.Add(snapshot.LockRetention);
                }
                catch (InvalidOperationException)
                {
                    unexpected.Add("missing timing snapshot after CompetitionLockTimeout");
                }
            }
            catch (InvalidOperationException)
            {
                invalidOps++;
                throw;
            }
            catch (Exception ex)
            {
                unexpected.Add(ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                endToEnd.Add(stopwatch.Elapsed);
            }
        }

        await stopApply.CancelAsync().ConfigureAwait(false);
        await applyLoop.ConfigureAwait(false);
        unexpected.Should().BeEmpty();

        return new ConcurrentRecordGoalReport(
            sampleCount,
            lockTimeouts,
            invalidOps,
            waits,
            retentions,
            endToEnd,
            unexpected.ToList());
    }

    private Task RunContinuousApplyProgressionAsync(
        ConcurrentTargets targets,
        ConcurrentBag<string> unexpected,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = fixture.Services.CreateScope();
                        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                        await executor.ApplyProgressionOutcomeAsync(
                            targets.ProgressionSourceStageId,
                            targets.ProgressionFixtureId,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ApplicationFailureException ex) when (
                        ex.Code == ApplicationErrorCodes.CompetitionLockTimeout)
                    {
                        // expected under contention
                    }
                    catch (Exception ex)
                    {
                        unexpected.Add("Apply loop: " + ex.GetType().Name + ": " + ex.Message);
                    }
                }
            },
            cancellationToken);
    }

    private async Task<StageId> PrepareForDematerializeAsync(string seed)
    {
        if (seed.StartsWith("swiss-8x3", StringComparison.Ordinal))
        {
            using var scope = fixture.Services.CreateScope();
            var shape = await CompetitionLockRetentionMeasureSupport.LoadPrimaryTemplateShapeAsync(scope)
                .ConfigureAwait(false);
            await PrepareSwissRoundForDematerializeAsync(shape.PrimaryStageId).ConfigureAwait(false);
            return shape.PrimaryStageId;
        }

        if (seed is "world-cup" or "coupe-de-france")
        {
            throw new InvalidOperationException(
                "compétition Completed — DematerializePhase refusé (aucune phase en exploitation avec matchs retirables).");
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var shape = await CompetitionLockRetentionMeasureSupport.LoadPrimaryTemplateShapeAsync(scope)
                .ConfigureAwait(false);
            return shape.PrimaryStageId;
        }
    }

    private async Task StripMatchesForRematerializeAsync(StageId stageId)
    {
        using var scope = fixture.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        await executor.DematerializePhaseAsync(stageId).ConfigureAwait(false);
    }

    private async Task PrepareSwissRoundForDematerializeAsync(StageId stageId)
    {
        using var scope = fixture.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var stage = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        stage.Should().NotBeNull();
        if (stage!.Status is StageStatus.Ready)
        {
            await executor.StartStageAsync(stageId).ConfigureAwait(false);
        }

        await executor.GenerateNextRoundAsync(stageId).ConfigureAwait(false);
    }

    private async Task ReloadSeedAsync(string seed)
    {
        var spec = SeedSpec.Parse(seed);
        if (TemplateSeedIds.Contains(spec.Id) || spec.Id is "world-cup" or "coupe-de-france")
        {
            await fixture.Services.GetRequiredService<TemplateRunner>()
                .ResetAndRunAsync([spec]).ConfigureAwait(false);
            return;
        }

        await fixture.Services.GetRequiredService<ScenarioRunner>()
            .ResetAndRunAsync([spec]).ConfigureAwait(false);
    }

    private async Task<CompetitionShape> LoadPrimaryShapeAsync()
    {
        using var scope = fixture.Services.CreateScope();
        return await CompetitionLockRetentionMeasureSupport.LoadPrimaryTemplateShapeAsync(scope).ConfigureAwait(false);
    }

    private async Task<CompetitionShape> LoadShapeForCupSfAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var summary = (await competitions.ListAsync().ConfigureAwait(false)).Single();
        var competition = await competitions.GetByIdReadOnlyAsync(summary.Id).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var matchCount = 0;
        foreach (var id in competition!.StageIds)
        {
            matchCount += (await matches.ListByStageForUpdateAsync(id).ConfigureAwait(false)).Count;
        }

        return new CompetitionShape(
            competition.Id,
            competition.StageIds[0],
            competition.Entries.Count,
            competition.StageIds.Count,
            matchCount);
    }

    private async Task<ProgressionTarget> ResolveCupSfProgressionTargetAsync()
    {
        var targets = await ResolveConcurrentTargetsAsync().ConfigureAwait(false);
        return new ProgressionTarget(targets.ProgressionSourceStageId, targets.ProgressionFixtureId);
    }

    private async Task<LiveMatchForRecordGoal> PrepareLiveMatchForRecordGoalAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var competitionId = (await competitions.ListAsync().ConfigureAwait(false)).Single().Id;
        var allMatches = new List<Match>();
        foreach (var stageId in (await competitions.GetByIdReadOnlyAsync(competitionId).ConfigureAwait(false))!.StageIds)
        {
            allMatches.AddRange(await matches.ListByStageForUpdateAsync(stageId).ConfigureAwait(false));
        }

        var target = allMatches.FirstOrDefault(match => match.Status == MatchStatus.Live)
                     ?? allMatches.First(match => match.Status == MatchStatus.Scheduled);
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        if (target.Status == MatchStatus.Scheduled)
        {
            var scheduledId = target.Id;
            unitOfWork.ClearTrackedChanges();
            await executor.StartMatchAsync(scheduledId).ConfigureAwait(false);
        }

        unitOfWork.ClearTrackedChanges();
        target = (await matches.GetByIdReadOnlyAsync(target.Id).ConfigureAwait(false))!;
        target.DeclaredParticipations.Should().NotBeEmpty();
        var participation = target.DeclaredParticipations[0];
        return new LiveMatchForRecordGoal(competitionId, target.Id, participation.Id);
    }

    private async Task<ConcurrentTargets> ResolveConcurrentTargetsAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var competitionId = (await competitions.ListAsync().ConfigureAwait(false)).Single().Id;
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId).ConfigureAwait(false);
        competition.Should().NotBeNull();

        var quarter = await stages.GetByIdReadOnlyAsync(competition!.StageIds[0], StageLoadProfile.Full)
            .ConfigureAwait(false);
        quarter.Should().NotBeNull();
        var fixtureId = quarter!.Rounds[0].Fixtures.OrderBy(item => item.Id.Value).First().Id;

        var semiId = competition.StageIds[^1];
        var semiMatches = await matches.ListByStageForUpdateAsync(semiId).ConfigureAwait(false);
        var scheduled = semiMatches.First(match => match.Status == MatchStatus.Scheduled);
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        unitOfWork.ClearTrackedChanges();
        await executor.StartMatchAsync(scheduled.Id).ConfigureAwait(false);
        unitOfWork.ClearTrackedChanges();
        var live = (await matches.GetByIdReadOnlyAsync(scheduled.Id).ConfigureAwait(false))!;
        live.DeclaredParticipations.Should().NotBeEmpty();
        var scorer = live.DeclaredParticipations[0].Id;

        return new ConcurrentTargets(
            competitionId,
            quarter.Id,
            fixtureId,
            live.Id,
            scorer);
    }

    private void WriteMeasurementTables(IReadOnlyList<MeasureRow> rows)
    {
        output.WriteLine(string.Empty);
        output.WriteLine("Attente du verrou — nearest-rank (ms)");
        output.WriteLine(new string('-', 120));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0,-40} | {1,-32} | {2,3} | {3,6:F1} | {4,6:F1} | {5,6:F1} | {6,5:P1}",
                "Opération",
                "Jeu",
                "n",
                "p50",
                "p95",
                "max",
                ">3s"));
        foreach (var row in rows)
        {
            output.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-40} | {1,-32} | {2,3} | {3,6:F1} | {4,6:F1} | {5,6:F1} | {6,5:P1}",
                    row.Operation,
                    row.Jeu,
                    row.SampleCount,
                    row.WaitP50.TotalMilliseconds,
                    row.WaitP95.TotalMilliseconds,
                    row.WaitMax.TotalMilliseconds,
                    row.WaitFractionOverThreeSeconds));
        }

        output.WriteLine(string.Empty);
        output.WriteLine("Détention du verrou — nearest-rank (ms)");
        output.WriteLine(new string('-', 120));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0,-40} | {1,-32} | {2,3} | {3,6:F1} | {4,6:F1} | {5,6:F1} | {6,5:P1}",
                "Opération",
                "Jeu",
                "n",
                "p50",
                "p95",
                "max",
                ">3s"));
        foreach (var row in rows)
        {
            output.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-40} | {1,-32} | {2,3} | {3,6:F1} | {4,6:F1} | {5,6:F1} | {6,5:P1}",
                    row.Operation,
                    row.Jeu,
                    row.SampleCount,
                    row.RetentionP50.TotalMilliseconds,
                    row.RetentionP95.TotalMilliseconds,
                    row.RetentionMax.TotalMilliseconds,
                    row.RetentionFractionOverThreeSeconds));
            output.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "    équipes={0}, phases={1}, matchs seed={2}",
                    row.TeamCount,
                    row.StageCount,
                    row.SeedMatchCount));
        }
    }

    private void WriteSkippedOperations(IReadOnlyList<SkippedMeasure> skipped)
    {
        output.WriteLine(string.Empty);
        output.WriteLine("Opérations non mesurées");
        foreach (var item in skipped)
        {
            output.WriteLine($"- {item.Operation} · {item.Jeu} : {item.Reason}");
        }
    }

    private void WriteConcurrentBlock(ConcurrentRecordGoalReport report)
    {
        output.WriteLine(string.Empty);
        output.WriteLine("RecordGoal concurrent (verrou réel via UseCaseExecutor)");
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "appels={0}, CompetitionLockTimeout={1}, InvalidOperationException={2}",
                report.Attempts,
                report.LockTimeoutCount,
                report.InvalidOperationExceptions));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "attente verrou p50={0:F1} ms p95={1:F1} ms max={2:F1} ms",
                NearestRankPercentiles.Percentile(report.LockWaits, 50).TotalMilliseconds,
                NearestRankPercentiles.Percentile(report.LockWaits, 95).TotalMilliseconds,
                NearestRankPercentiles.Max(report.LockWaits).TotalMilliseconds));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "détention verrou p50={0:F1} ms p95={1:F1} ms max={2:F1} ms",
                NearestRankPercentiles.Percentile(report.LockRetentions, 50).TotalMilliseconds,
                NearestRankPercentiles.Percentile(report.LockRetentions, 95).TotalMilliseconds,
                NearestRankPercentiles.Max(report.LockRetentions).TotalMilliseconds));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "latence bout-en-bout p50={0:F1} ms p95={1:F1} ms max={2:F1} ms",
                NearestRankPercentiles.Percentile(report.EndToEndLatencies, 50).TotalMilliseconds,
                NearestRankPercentiles.Percentile(report.EndToEndLatencies, 95).TotalMilliseconds,
                NearestRankPercentiles.Max(report.EndToEndLatencies).TotalMilliseconds));
    }
}
