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
                new TieFormat(TieFormat.SingleLeg, aggregateScoring: false));
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

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await ReapplyBackfillSqlAsync(context).ConfigureAwait(false);
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

    private static async Task ReapplyBackfillSqlAsync(PlayUpDbContext context)
    {
        var defaultOneLegJson = TieFormatJson.Serialize(TieFormat.DefaultOneLeg);
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE rounds AS r
            SET tie_format = {0}::jsonb
            WHERE r.tie_format IS NULL
              AND r.id IN (
                SELECT DISTINCT fr.round_id
                FROM fixtures AS fr
                INNER JOIN fixture_attachments AS fa ON fa.fixture_id = fr.id
                WHERE fr.round_id IS NOT NULL);
            """,
            defaultOneLegJson).ConfigureAwait(false);
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
