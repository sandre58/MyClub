// -----------------------------------------------------------------------
// <copyright file="BackfillMaterializedRoundTieFormatMigrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public sealed class BackfillMaterializedRoundTieFormatMigrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261123220016_AddSlotsStageEntryDeferrableUnique";
    private const string TargetMigration = "20261127212737_BackfillMaterializedRoundTieFormat";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Backfill_stamps_only_materialized_null_rounds_and_sql_up_is_idempotentAsync()
    {
        RoundId materializedRoundId;
        RoundId emptyRoundId;
        RoundId overrideRoundId;
        StageId stageId;

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBefore).ConfigureAwait(false);

            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Legacy cup"), SampleRegulations.Standard(), _clock);
            var home = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
            var away = competition.AddEntry(TeamId.New(), "Beta", _clock).Id;

            var stageRegulation = new StageRegulation(
                SampleRegulations.Standard().MatchRules,
                standingRules: null,
                new TieFormat(
                    TieFormat.TwoLegs,
                    aggregateScoring: true,
                    penaltyShootoutRule: new PenaltyShootoutRule()));
            var stage = Stage.Create(competition.Id, new StageName("Cup"), stageRegulation, _clock);
            competition.AddStage(stage.Id, _clock);

            var materializedRound = stage.AddRound("QF", _clock);
            var emptyRound = stage.AddRound("SF", _clock);
            var overrideRound = stage.AddRound(
                "Final",
                new TieFormat(TieFormat.TwoLegs, aggregateScoring: true),
                _clock);

            materializedRoundId = materializedRound.Id;
            emptyRoundId = emptyRound.Id;
            overrideRoundId = overrideRound.Id;
            stageId = stage.Id;

            const string slotA = "QF-A";
            const string slotB = "QF-B";
            stage.AddSlot(slotA);
            stage.AddSlot(slotB);
            stage.SeedEntryRoundBracketPairs();
            var pairKey = stage.BracketPairs[0].PairKey;
            stage.ApplyResolvedEntry(slotA, home, _clock);
            stage.ApplyResolvedEntry(slotB, away, _clock);

            var fixture = stage.AddFixtureForMatchProduction(
                materializedRoundId,
                _clock,
                slotA,
                slotB,
                pairKey);
            var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
            stage.AttachMatchForMatchProduction(fixture.Id, match.Id, legIndex: 1, _clock);

            competitions.Add(competition);
            stages.Add(stage);
            matches.Add(match);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

            materializedRound.TieFormat.Should().BeNull();
            emptyRound.TieFormat.Should().BeNull();
            overrideRound.TieFormat.Should().NotBeNull();
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        await AssertRoundTieFormatsAsync(provider, stageId, materializedRoundId, emptyRoundId, overrideRoundId)
            .ConfigureAwait(false);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBefore).ConfigureAwait(false);
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        await AssertRoundTieFormatsAsync(provider, stageId, materializedRoundId, emptyRoundId, overrideRoundId)
            .ConfigureAwait(false);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var applied = (await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).ToArray();
            applied.Should().Contain(TargetMigration);
        }
    }

    [IntegrationFact]
    public async Task Backfill_three_stage_regimes_preserve_inheritance_and_stamp_materialized_onlyAsync()
    {
        CompetitionId competitionId;
        StageId readyNoMatchStageId;
        RoundId readyNoMatchRoundId;
        StageId readyMaterializedStageId;
        RoundId readyMaterializedRoundId;
        StageId runningStageId;
        RoundId runningMaterializedRoundId;

        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        var decisiveDefault = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            penaltyShootoutRule: new PenaltyShootoutRule());

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBefore).ConfigureAwait(false);

            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("A1 three phases"), SampleRegulations.Standard(), _clock);
            competitionId = competition.Id;

            var readyNoMatch = SeedReadyCupWithoutMatch(
                competition,
                "Ready sans match",
                decisiveDefault,
                _clock,
                out readyNoMatchRoundId);
            readyNoMatchStageId = readyNoMatch.Id;

            var readyMaterialized = SeedReadyCupMaterialized(
                competition,
                "Ready matérialisée",
                decisiveDefault,
                matches,
                _clock,
                out readyMaterializedRoundId);
            readyMaterializedStageId = readyMaterialized.StageId;

            var running = SeedRunningCupMaterialized(
                competition,
                "Running",
                decisiveDefault,
                matches,
                _clock,
                out runningMaterializedRoundId);
            runningStageId = running.StageId;

            competitions.Add(competition);
            stages.Add(readyNoMatch);
            stages.Add(readyMaterialized.Stage);
            stages.Add(running.Stage);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

            readyNoMatch.Status.Should().Be(StageStatus.Ready);
            readyMaterialized.Stage.Status.Should().Be(StageStatus.Ready);
            running.Stage.Status.Should().Be(StageStatus.Running);
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        await AssertThreePhasePostMigrationAsync(
                provider,
                competitionId,
                readyNoMatchStageId,
                readyNoMatchRoundId,
                readyMaterializedStageId,
                readyMaterializedRoundId,
                runningStageId,
                runningMaterializedRoundId,
                decisiveDefault,
                _clock)
            .ConfigureAwait(false);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBefore).ConfigureAwait(false);
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        await AssertThreePhasePostMigrationAsync(
                provider,
                competitionId,
                readyNoMatchStageId,
                readyNoMatchRoundId,
                readyMaterializedStageId,
                readyMaterializedRoundId,
                runningStageId,
                runningMaterializedRoundId,
                decisiveDefault,
                _clock)
            .ConfigureAwait(false);
    }

    private static Stage SeedReadyCupWithoutMatch(
        Competition competition,
        string name,
        TieFormat defaultTie,
        IClock clock,
        out RoundId entryRoundId)
    {
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            defaultTie);
        var stage = Stage.Create(competition.Id, new StageName(name), regulation, clock);
        competition.AddStage(stage.Id, clock);
        var round = stage.AddRound("Tour", clock);
        entryRoundId = round.Id;
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        var home = competition.AddEntry(TeamId.New(), $"{name}-H", clock).Id;
        var away = competition.AddEntry(TeamId.New(), $"{name}-A", clock).Id;
        stage.ApplyResolvedEntry("S1", home, clock);
        stage.ApplyResolvedEntry("S2", away, clock);
        stage.Prepare(clock);
        return stage;
    }

    private static (Stage Stage, StageId StageId) SeedReadyCupMaterialized(
        Competition competition,
        string name,
        TieFormat defaultTie,
        IMatchRepository matches,
        IClock clock,
        out RoundId materializedRoundId)
    {
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            defaultTie);
        var stage = Stage.Create(competition.Id, new StageName(name), regulation, clock);
        competition.AddStage(stage.Id, clock);
        var round = stage.AddRound("Tour", clock);
        materializedRoundId = round.Id;
        stage.AddSlot("M1");
        stage.AddSlot("M2");
        stage.SeedEntryRoundBracketPairs();
        var pairKey = stage.BracketPairs[0].PairKey;
        var home = competition.AddEntry(TeamId.New(), $"{name}-H", clock).Id;
        var away = competition.AddEntry(TeamId.New(), $"{name}-A", clock).Id;
        stage.ApplyResolvedEntry("M1", home, clock);
        stage.ApplyResolvedEntry("M2", away, clock);
        var fixture = stage.AddFixtureForMatchProduction(round.Id, clock, "M1", "M2", pairKey);
        var match = Match.Create(competition.Id, stage.Id, home, away, clock);
        stage.AttachMatchForMatchProduction(fixture.Id, match.Id, legIndex: 1, clock);
        stage.Prepare(clock);
        matches.Add(match);
        return (stage, stage.Id);
    }

    private static (Stage Stage, StageId StageId) SeedRunningCupMaterialized(
        Competition competition,
        string name,
        TieFormat defaultTie,
        IMatchRepository matches,
        IClock clock,
        out RoundId materializedRoundId)
    {
        var seeded = SeedReadyCupMaterialized(competition, name, defaultTie, matches, clock, out materializedRoundId);
        StartStage.Execute(seeded.Stage, clock);
        return seeded;
    }

    private static async Task AssertThreePhasePostMigrationAsync(
        ServiceProvider provider,
        CompetitionId competitionId,
        StageId readyNoMatchStageId,
        RoundId readyNoMatchRoundId,
        StageId readyMaterializedStageId,
        RoundId readyMaterializedRoundId,
        StageId runningStageId,
        RoundId runningMaterializedRoundId,
        TieFormat decisiveDefault,
        IClock clock)
    {
        using var scope = provider.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = await competitions.GetByIdForUpdateAsync(competitionId).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var readyNoMatch = await stages.GetByIdForUpdateAsync(readyNoMatchStageId).ConfigureAwait(false);
        var readyMaterialized = await stages.GetByIdForUpdateAsync(readyMaterializedStageId).ConfigureAwait(false);
        var running = await stages.GetByIdForUpdateAsync(runningStageId).ConfigureAwait(false);

        readyNoMatch.Should().NotBeNull();
        readyMaterialized.Should().NotBeNull();
        running.Should().NotBeNull();

        var readyNoMatchRound = readyNoMatch!.Rounds.Single(round => round.Id.Equals(readyNoMatchRoundId));
        readyNoMatchRound.TieFormat.Should().BeNull();
        Stage.ResolveEffectiveTieFormat(readyNoMatchRound, readyNoMatch.Regulation)
            .Should()
            .BeEquivalentTo(decisiveDefault);
        readyNoMatch!.Status.Should().Be(StageStatus.Ready);
        var materializeReadyNoMatch = () => MaterializeCupFromOccupiedSlots.Execute(
            competition!,
            readyNoMatch,
            [],
            [],
            clock,
            planEntireRound: true);
        materializeReadyNoMatch.Should().NotThrow();
        var prepareAgain = () => PrepareStage.Execute(readyNoMatch, [readyNoMatch], clock);
        prepareAgain.Should().Throw<DomainException>();

        var readyMatRound = readyMaterialized!.Rounds.Single(round => round.Id.Equals(readyMaterializedRoundId));
        readyMatRound.TieFormat.Should().BeEquivalentTo(TieFormat.DefaultOneLeg);
        readyMaterialized.Status.Should().Be(StageStatus.Ready);

        var runningRound = running!.Rounds.Single(round => round.Id.Equals(runningMaterializedRoundId));
        runningRound.TieFormat.Should().BeEquivalentTo(TieFormat.DefaultOneLeg);
        running.Status.Should().Be(StageStatus.Running);
        runningRound.Fixtures.Should().ContainSingle(fixture =>
            fixture.Attachments.Count > 0);
    }

    private static async Task AssertRoundTieFormatsAsync(
        ServiceProvider provider,
        StageId stageId,
        RoundId materializedRoundId,
        RoundId emptyRoundId,
        RoundId overrideRoundId)
    {
        using var scope = provider.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var reloaded = await stages.GetByIdForUpdateAsync(stageId).ConfigureAwait(false);
        reloaded.Should().NotBeNull();

        var materialized = reloaded!.Rounds.Single(round => round.Id.Equals(materializedRoundId));
        var empty = reloaded.Rounds.Single(round => round.Id.Equals(emptyRoundId));
        var overriding = reloaded.Rounds.Single(round => round.Id.Equals(overrideRoundId));

        materialized.TieFormat.Should().BeEquivalentTo(TieFormat.DefaultOneLeg);
        empty.TieFormat.Should().BeNull();
        overriding.TieFormat!.NumberOfLegs.Should().Be(TieFormat.TwoLegs);

        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
        var json = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT tie_format AS "Value"
                FROM rounds
                WHERE id = {0}
                """,
                materializedRoundId.Value)
            .SingleAsync()
            .ConfigureAwait(false);
        TieFormatJson.Deserialize(json).Should().BeEquivalentTo(TieFormat.DefaultOneLeg);
    }
}
