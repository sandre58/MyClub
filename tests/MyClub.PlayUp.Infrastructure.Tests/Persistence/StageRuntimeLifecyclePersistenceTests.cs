// -----------------------------------------------------------------------
// <copyright file="StageRuntimeLifecyclePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class StageRuntimeLifecyclePersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Draw_published_inputs_and_resolution_round_trip_on_postgresAsync()
    {
        StageId stageId;
        DrawId drawId;
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var inputs = DrawInputs.ForSlot(
            [entryA, entryB],
            new SeedMap(new Dictionary<EntryId, int> { [entryA] = 1, [entryB] = 2 }),
            new PotMembership(new Dictionary<EntryId, int> { [entryA] = 1 }),
            [new SlotDrawPlacement(entryA, "SF1")]);
        var resolution = DrawResolution.ResolvedSlots(
        [
            new SlotDrawPlacement(entryA, "SF1"),
            new SlotDrawPlacement(entryB, "SF2")
        ]);

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
            stage.AddSlot("SF1", _clock);
            stage.AddSlot("SF2", _clock);
            var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.ConfigureDrawInputs(draw.Id, inputs, _clock);
            stage.RecordDrawResolution(draw.Id, resolution, _clock);
            stage.PublishDraw(draw.Id, _clock);
            stageId = stage.Id;
            drawId = draw.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            var draw = loaded.Draws.Single(candidate => candidate.Id == drawId);
            draw.Status.Should().Be(DrawStatus.Published);
            draw.Inputs!.Entries.Should().Equal(entryA, entryB);
            draw.Inputs.SeedMap!.Seeds[entryA].Should().Be(1);
            draw.Resolution.SlotResults.Should().Equal(
                new SlotDrawPlacement(entryA, "SF1"),
                new SlotDrawPlacement(entryB, "SF2"));
        }
    }

    [IntegrationFact]
    public async Task Draw_no_solution_and_cancelled_round_trip_on_postgresAsync()
    {
        StageId stageId;
        DrawId noSolutionId;
        DrawId cancelledId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
            var entry = EntryId.New();
            var noSolution = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.ConfigureDrawInputs(noSolution.Id, DrawInputs.ForSlot([entry]), _clock);
            stage.MarkDrawNoSolution(noSolution.Id, _clock);

            var cancelled = stage.CreateDraw(DrawResolutionKind.Group, _clock);
            stage.CancelDraw(cancelled.Id, _clock);

            stageId = stage.Id;
            noSolutionId = noSolution.Id;
            cancelledId = cancelled.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Draws.Single(draw => draw.Id == noSolutionId).Resolution.State.Should()
                .Be(DrawResolutionState.NoSolution);
            loaded.Draws.Single(draw => draw.Id == cancelledId).Status.Should().Be(DrawStatus.Cancelled);
        }
    }

    [IntegrationFact]
    public async Task Penalty_create_remove_round_trip_on_postgresAsync()
    {
        StageId stageId;
        var entryId = EntryId.New();

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
            stage.AddPenalty(entryId, 2, _clock, "Admin");
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var loaded = await stages.GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Penalties.Should().ContainSingle();
            loaded.RemovePenalty(loaded.Penalties[0].Id, _clock);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Penalties.Should().BeEmpty();
        }
    }

    [IntegrationFact]
    public async Task MatchPlacement_round_trip_on_postgresAsync()
    {
        StageId stageId;
        MatchId matchId;
        var resourceId = ResourceId.New();
        var start = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Final", _clock);
            var addFixture = stage.AddFixture(round.Id, _clock);
            var match = Match.Create(competition.Id, stage.Id, EntryId.New(), EntryId.New(), _clock);
            matchId = match.Id;
            stage.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
            stage.ApplyMatchPlacements([new MatchPlacement(match.Id, start, resourceId)], [match.Id]);
            stageId = stage.Id;
            stages.Add(stage);
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.MatchPlacements.Should().ContainSingle()
                .Which.Should().Be(new MatchPlacement(matchId, start, resourceId));
        }
    }

    [IntegrationFact]
    public async Task Delete_match_with_placement_is_restrictedAsync()
    {
        MatchId matchId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Final", _clock);
            var addFixture = stage.AddFixture(round.Id, _clock);
            var match = Match.Create(competition.Id, stage.Id, EntryId.New(), EntryId.New(), _clock);
            matchId = match.Id;
            stage.AttachMatch(addFixture.Id, match.Id, 1, _clock);
            stage.ApplyMatchPlacements(
                [new MatchPlacement(match.Id, DateTimeOffset.UtcNow, ResourceId.New())],
                [match.Id]);
            stages.Add(stage);
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

            // Isolate match_placements RESTRICT from fixture_attachments RESTRICT.
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM fixture_attachments WHERE match_id = {0}",
                matchId.Value);

            var act = async () => await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM matches WHERE id = {0}",
                matchId.Value);

            var exception = await act.Should().ThrowAsync<PostgresException>();
            exception.Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
            exception.Which.ConstraintName.Should().Be("FK_match_placements_matches_match_id");
        }
    }

    [IntegrationFact]
    public async Task Delete_stage_cascades_draws_and_penalties_without_matchAsync()
    {
        StageId stageId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
            stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.AddPenalty(EntryId.New(), 1, _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var stage = await context.Set<Stage>().SingleAsync(candidate => candidate.Id == stageId);
            context.Remove(stage);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var drawCount = await context.Database.SqlQueryRaw<CountRow>(
                    """
                    SELECT COUNT(*)::int AS "Value"
                    FROM draws
                    WHERE stage_id = {0}
                    """,
                    stageId.Value)
                .SingleAsync();
            var penaltyCount = await context.Database.SqlQueryRaw<CountRow>(
                    """
                    SELECT COUNT(*)::int AS "Value"
                    FROM penalties
                    WHERE stage_id = {0}
                    """,
                    stageId.Value)
                .SingleAsync();
            drawCount.Value.Should().Be(0);
            penaltyCount.Value.Should().Be(0);
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    private sealed record CountRow(int Value);
}
