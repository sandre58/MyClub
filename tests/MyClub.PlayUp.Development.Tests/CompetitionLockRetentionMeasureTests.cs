// -----------------------------------------------------------------------
// <copyright file="CompetitionLockRetentionMeasureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
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
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// A10-R7 lot 1 — measure competition advisory lock wait and retention (p50/p95/max) on exclusive writes.
/// Production code is unchanged; <see cref="UseCaseExecutor.RecordGoalAsync"/> is wrapped in
/// <see cref="IUnitOfWork.ExecuteExclusiveAsync"/> only for the concurrent RecordGoal emulation (Stéphane Q1, 23/11/2026).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
[Trait("Category", "PerformanceBaseline")]
public sealed class CompetitionLockRetentionMeasureTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    private static readonly HashSet<string> TemplateSeedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "ligue-1",
        "champions-league",
        "world-cup",
        "coupe-de-france"
    };

    [Fact]
    public async Task Measure_competition_lock_retention_campaignAsync()
    {
        var sampleCount = CompetitionLockRetentionMeasureSupport.ResolveSampleCount();
        output.WriteLine($"PLAYUP_LOCK_MEASURE_SAMPLES={sampleCount}");

        var retentionRows = new List<MeasureRow>();
        var skipped = new List<SkippedMeasure>();

        await MeasureApplyProgressionAsync(sampleCount, retentionRows, skipped);
        await MeasureDematerializeSwissAsync(sampleCount, retentionRows, skipped);
        await MeasureMaterializeAcrossTemplatesAsync(sampleCount, retentionRows, skipped);
        await CompetitionLockRetentionMeasureSupport.AppendConfirmOperationsNotMeasuredAsync(skipped);

        var recordGoalSqlBaseline = await MeasureRecordGoalSqlBaselineAsync();
        var concurrent = await MeasureConcurrentRecordGoalAsync(sampleCount);

        WriteRetentionTable(retentionRows);
        WriteSkippedOperations(skipped);
        WriteConcurrentBlock(concurrent);
        output.WriteLine($"RecordGoal SQL baseline (sans verrou) : {recordGoalSqlBaseline} commandes EF");

        retentionRows.Should().NotBeEmpty("at least one exclusive operation must be measured");
        concurrent.Attempts.Should().BeGreaterThan(0);
        concurrent.InvalidOperationExceptions.Should().Be(0);
        concurrent.UnexpectedFailures.Should().BeEmpty();
    }

    private async Task MeasureApplyProgressionAsync(
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        const string applySeed = "freshness-actions-recette";
        const string applyJeu = "freshness-actions-recette (14 phases · 5 équipes)";
        try
        {
            var shape = await LoadShapeAfterSeedAsync(applySeed).ConfigureAwait(false);
            var retentions = new List<TimeSpan>(sampleCount);
            for (var index = 0; index < sampleCount; index++)
            {
                await ReloadSeedAsync(applySeed).ConfigureAwait(false);
                var progression = await ResolveFreshnessProgressionTargetAsync().ConfigureAwait(false);
                retentions.Add(await MeasureRetentionForApplyProgressionAsync(
                    progression.SourceStageId,
                    progression.FixtureId).ConfigureAwait(false));
            }

            rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                "ApplyProgressionOutcomeAsync",
                applyJeu,
                shape,
                retentions));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            skipped.Add(new SkippedMeasure("ApplyProgressionOutcomeAsync", applyJeu, ex.Message));
        }
    }

    private async Task MeasureDematerializeSwissAsync(
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        const string seed = "swiss-8x3:prepared";
        const string jeu = "swiss-8x3 (8 équipes · 3 tours)";
        try
        {
            var shape = await LoadShapeAfterSeedAsync(seed).ConfigureAwait(false);
            var swissStageId = shape.PrimaryStageId;
            var retentions = new List<TimeSpan>(sampleCount);
            for (var index = 0; index < sampleCount; index++)
            {
                await ReloadSeedAsync(seed).ConfigureAwait(false);
                await PrepareSwissRoundForDematerializeAsync(swissStageId).ConfigureAwait(false);
                retentions.Add(await MeasureRetentionForDematerializeAsync(swissStageId).ConfigureAwait(false));
            }

            rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                "DematerializePhaseAsync (Suisse · dernier tour)",
                jeu,
                shape,
                retentions));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            skipped.Add(new SkippedMeasure("DematerializePhaseAsync", jeu, ex.Message));
        }
    }

    private async Task MeasureMaterializeAcrossTemplatesAsync(
        int sampleCount,
        List<MeasureRow> rows,
        List<SkippedMeasure> skipped)
    {
        foreach (var (seed, jeu) in new[]
                 {
                     ("structure-materialize-recette", "structure-materialize-recette (RECETTE G1 · 8 équipes)")
                 })
        {
            try
            {
                var shape = await LoadShapeAfterSeedAsync(seed).ConfigureAwait(false);
                var retentions = new List<TimeSpan>(sampleCount);
                for (var index = 0; index < sampleCount; index++)
                {
                    await ReloadSeedAsync(seed).ConfigureAwait(false);
                    retentions.Add(await MeasureRetentionForMaterializeAsync(shape.PrimaryStageId).ConfigureAwait(false));
                }

                rows.Add(CompetitionLockRetentionMeasureSupport.BuildRow(
                    "MaterializeMatchesAsync",
                    jeu,
                    shape,
                    retentions));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                skipped.Add(new SkippedMeasure("MaterializeMatchesAsync", jeu, ex.Message));
            }
        }

        foreach (var (seed, jeu, reason) in new[]
                 {
                     (
                         "ligue-1:prepared",
                         "ligue-1 (18 équipes)",
                         "MaterializeMatches refuse une compétition Running déjà matérialisée (guard StageMaterializeGuard)"),
                     (
                         "champions-league:prepared",
                         "champions-league (32 équipes)",
                         "idem — template préparé = compétition Running et matchs déjà présents"),
                     (
                         "world-cup",
                         "world-cup (32 équipes)",
                         "template terminé (Completed) — pas de matérialisation rejouable"),
                     (
                         "coupe-de-france",
                         "coupe-de-france (32 équipes)",
                         "template terminé (Completed) — pas de matérialisation rejouable")
                 })
        {
            try
            {
                await LoadShapeAfterSeedAsync(seed).ConfigureAwait(false);
                skipped.Add(new SkippedMeasure("MaterializeMatchesAsync", jeu, reason));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                skipped.Add(new SkippedMeasure("MaterializeMatchesAsync", jeu, $"{reason} ({ex.Message})"));
            }
        }
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
        var waits = new List<TimeSpan>(sampleCount);
        var endToEnd = new List<TimeSpan>(sampleCount);
        var lockTimeouts = 0;
        var invalidOps = 0;
        var unexpected = new List<string>();

        for (var index = 0; index < sampleCount; index++)
        {
            await ReloadSeedAsync("cup-sf-running").ConfigureAwait(false);
            var targets = await ResolveConcurrentTargetsAsync().ConfigureAwait(false);

            var applyTask = Task.Run(async () =>
            {
                using var scope = fixture.Services.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                await executor.ApplyProgressionOutcomeAsync(
                    targets.ProgressionSourceStageId,
                    targets.ProgressionFixtureId).ConfigureAwait(false);
            });

            await Task.Yield();

            using (var scope = fixture.Services.CreateScope())
            {
                var timing = scope.ServiceProvider.GetRequiredService<CompetitionAdvisoryLockTimingInterceptor>();
                timing.Reset();
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await CompetitionLockRetentionMeasureSupport.EmulateExclusiveRecordGoalAsync(
                        scope,
                        targets.CompetitionId,
                        targets.RecordGoalMatchId,
                        targets.ScorerMemberId).ConfigureAwait(false);
                }
                catch (ApplicationFailureException ex) when (
                    ex.Code == ApplicationErrorCodes.CompetitionLockTimeout)
                {
                    lockTimeouts++;
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
                    waits.Add(timing.LastSnapshot?.LockWait ?? TimeSpan.Zero);
                }
            }

            await applyTask.ConfigureAwait(false);
        }

        return new ConcurrentRecordGoalReport(
            sampleCount,
            lockTimeouts,
            invalidOps,
            waits,
            endToEnd,
            unexpected);
    }

    private async Task<TimeSpan> MeasureRetentionForApplyProgressionAsync(StageId stageId, FixtureId fixtureId) =>
        await MeasureExclusiveRetentionAsync(
            async scope =>
            {
                var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                await executor.ApplyProgressionOutcomeAsync(stageId, fixtureId).ConfigureAwait(false);
            }).ConfigureAwait(false);

    private async Task<TimeSpan> MeasureRetentionForMaterializeAsync(StageId stageId) =>
        await MeasureExclusiveRetentionAsync(
            async scope =>
            {
                var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                await executor.MaterializeMatchesAsync(stageId).ConfigureAwait(false);
            }).ConfigureAwait(false);

    private async Task<TimeSpan> MeasureRetentionForDematerializeAsync(StageId stageId) =>
        await MeasureExclusiveRetentionAsync(
            async scope =>
            {
                var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
                await executor.DematerializePhaseAsync(stageId).ConfigureAwait(false);
            }).ConfigureAwait(false);

    private async Task<TimeSpan> MeasureExclusiveRetentionAsync(Func<IServiceScope, Task> operation)
    {
        using var scope = fixture.Services.CreateScope();
        var timing = scope.ServiceProvider.GetRequiredService<CompetitionAdvisoryLockTimingInterceptor>();
        timing.Reset();
        var stopwatch = Stopwatch.StartNew();
        await operation(scope).ConfigureAwait(false);
        stopwatch.Stop();
        return timing.LastSnapshot?.LockRetention ?? stopwatch.Elapsed;
    }

    private async Task ReloadSeedAsync(string seed)
    {
        var spec = SeedSpec.Parse(seed);
        if (TemplateSeedIds.Contains(spec.Id))
        {
            await fixture.Services.GetRequiredService<TemplateRunner>()
                .ResetAndRunAsync([spec]).ConfigureAwait(false);
            return;
        }

        await fixture.Services.GetRequiredService<ScenarioRunner>()
            .ResetAndRunAsync([spec]).ConfigureAwait(false);
    }

    private async Task<CompetitionShape> LoadShapeAfterSeedAsync(string seed)
    {
        await ReloadSeedAsync(seed).ConfigureAwait(false);
        using var scope = fixture.Services.CreateScope();
        return await CompetitionLockRetentionMeasureSupport.LoadShapeFromScopeAsync(scope).ConfigureAwait(false);
    }

    private async Task<ProgressionTarget> ResolveFreshnessProgressionTargetAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = await competitions.GetByIdForUpdateAsync(
            (await competitions.ListAsync().ConfigureAwait(false))
            .Single(item => item.Name.Value == FreshnessActionsRecetteNames.Competition).Id).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var stage = (await stages.GetByIdsForUpdateAsync(competition!.StageIds).ConfigureAwait(false))
            .Single(item => item.Name.Value == "Barrage");
        var fixtureId = stage.Rounds[0].Fixtures.OrderBy(item => item.Id.Value).Last().Id;
        return new ProgressionTarget(stage.Id, fixtureId);
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
        if (target.Status == MatchStatus.Scheduled)
        {
            await executor.StartMatchAsync(target.Id).ConfigureAwait(false);
            target = (await matches.GetByIdForUpdateAsync(target.Id).ConfigureAwait(false))!;
        }

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
        await executor.StartMatchAsync(scheduled.Id).ConfigureAwait(false);
        var live = (await matches.GetByIdForUpdateAsync(scheduled.Id).ConfigureAwait(false))!;
        live.DeclaredParticipations.Should().NotBeEmpty();
        var scorer = live.DeclaredParticipations[0].Id;

        return new ConcurrentTargets(
            competitionId,
            quarter.Id,
            fixtureId,
            live.Id,
            scorer);
    }

    private void WriteRetentionTable(IReadOnlyList<MeasureRow> rows)
    {
        output.WriteLine(string.Empty);
        output.WriteLine("Détention du verrou — nearest-rank (ms)");
        output.WriteLine(new string('-', 110));
        output.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0,-46} | {1,-28} | {2,4} | {3,3} | {4,6:F1} | {5,6:F1} | {6,6:F1} | {7,5:P1}",
                "Opération",
                "Jeu",
                "n",
                "éq.",
                "p50",
                "p95",
                "max",
                ">3s"));
        foreach (var row in rows)
        {
            output.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-46} | {1,-28} | {2,4} | {3,3} | {4,6:F1} | {5,6:F1} | {6,6:F1} | {7,5:P1}",
                    row.Operation,
                    row.Jeu,
                    row.SampleCount,
                    row.TeamCount,
                    row.P50.TotalMilliseconds,
                    row.P95.TotalMilliseconds,
                    row.Max.TotalMilliseconds,
                    row.FractionOverThreeSeconds));
            output.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "    phases={0}, matchs seed={1}",
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
        output.WriteLine("RecordGoal concurrent (émulation ExecuteExclusiveAsync côté test)");
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
                "latence bout-en-bout p50={0:F1} ms p95={1:F1} ms max={2:F1} ms",
                NearestRankPercentiles.Percentile(report.EndToEndLatencies, 50).TotalMilliseconds,
                NearestRankPercentiles.Percentile(report.EndToEndLatencies, 95).TotalMilliseconds,
                NearestRankPercentiles.Max(report.EndToEndLatencies).TotalMilliseconds));
    }
}
