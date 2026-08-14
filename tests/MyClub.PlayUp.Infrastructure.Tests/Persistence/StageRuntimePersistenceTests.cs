// -----------------------------------------------------------------------
// <copyright file="StageRuntimePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class StageRuntimePersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Draw_publish_preserves_non_default_status_inputs_and_resolutionAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        StageId stageId;
        DrawId drawId;
        var inputs = DrawInputs.ForSlot(
            [entryA, entryB],
            new SeedMap(new Dictionary<EntryId, int> { [entryA] = 1 }),
            fixedPlacements: [new SlotDrawPlacement(entryA, "W1")]);
        var resolution = DrawResolution.ResolvedSlots(
        [
            new SlotDrawPlacement(entryA, "W1"),
            new SlotDrawPlacement(entryB, "W2")
        ]);

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            stage.AddSlot("W1", _clock);
            stage.AddSlot("W2", _clock);
            var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.ConfigureDrawInputs(draw.Id, inputs, _clock);
            stage.RecordDrawResolution(draw.Id, resolution, _clock);
            stage.PublishDraw(draw.Id, _clock);
            stageId = stage.Id;
            drawId = draw.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            var draw = loaded.Draws.Should().ContainSingle(candidate => candidate.Id == drawId).Subject;
            draw.Status.Should().Be(DrawStatus.Published);
            draw.Status.Should().NotBe(DrawStatus.Draft);
            draw.Kind.Should().Be(DrawResolutionKind.Slot);
            draw.Inputs!.Entries.Should().Equal(entryA, entryB);
            draw.Inputs.FixedSlots.Should().Equal(new SlotDrawPlacement(entryA, "W1"));
            draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
            draw.Resolution.SlotResults.Should().Equal(
                new SlotDrawPlacement(entryA, "W1"),
                new SlotDrawPlacement(entryB, "W2"));
        }
    }

    [Fact]
    public async Task Draw_no_solution_and_cancel_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        StageId stageId;
        DrawId noSolutionId;
        DrawId cancelledId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var entry = EntryId.New();
            var noSolution = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.ConfigureDrawInputs(noSolution.Id, DrawInputs.ForSlot([entry]), _clock);
            stage.MarkDrawNoSolution(noSolution.Id, _clock);

            var cancelled = stage.CreateDraw(DrawResolutionKind.Group, _clock);
            stage.CancelDraw(cancelled.Id, _clock);

            stageId = stage.Id;
            noSolutionId = noSolution.Id;
            cancelledId = cancelled.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Draws.Single(draw => draw.Id == noSolutionId).Resolution.State.Should().Be(DrawResolutionState.NoSolution);
            loaded.Draws.Single(draw => draw.Id == cancelledId).Status.Should().Be(DrawStatus.Cancelled);
        }
    }

    [Fact]
    public async Task Penalty_add_and_remove_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var entryId = EntryId.New();
        StageId stageId;
        PenaltyId keptId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("League"), SampleRegulations.Standard(), _clock);
            var kept = stage.AddPenalty(entryId, 3, _clock, "Fair play");
            var removed = stage.AddPenalty(EntryId.New(), 1, _clock);
            stage.RemovePenalty(removed.Id, _clock);
            stageId = stage.Id;
            keptId = kept.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            var penalty = loaded.Penalties.Should().ContainSingle().Subject;
            penalty.Id.Should().Be(keptId);
            penalty.EntryId.Should().Be(entryId);
            penalty.PointsDeducted.Should().Be(3);
            penalty.Reason.Should().Be("Fair play");
        }
    }

    [Fact]
    public async Task Draw_domain_mutations_mark_status_inputs_and_resolution_modifiedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        StageId stageId;
        DrawId drawId;
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var inputs = DrawInputs.ForSlot(
            [entryA, entryB],
            fixedPlacements: [new SlotDrawPlacement(entryA, "W1")]);
        var resolution = DrawResolution.ResolvedSlots(
        [
            new SlotDrawPlacement(entryA, "W1"),
            new SlotDrawPlacement(entryB, "W2")
        ]);

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            stage.AddSlot("W1", _clock);
            stage.AddSlot("W2", _clock);
            var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stageId = stage.Id;
            drawId = draw.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.ConfigureDrawInputs(drawId, inputs, _clock);
            loaded.RecordDrawResolution(drawId, resolution, _clock);
            loaded.PublishDraw(drawId, _clock);

            var drawEntry = context.Entry(loaded.Draws.Single(candidate => candidate.Id == drawId));
            drawEntry.Property(draw => draw.Status).IsModified.Should().BeTrue();
            drawEntry.Property(draw => draw.Inputs).IsModified.Should().BeTrue();
            drawEntry.Property(draw => draw.Resolution).IsModified.Should().BeTrue();

            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new StageRepository(context).GetByIdAsync(stageId);
            reloaded.Should().NotBeNull();
            var draw = reloaded.Draws.Single(candidate => candidate.Id == drawId);
            draw.Status.Should().Be(DrawStatus.Published);
            draw.Inputs!.Entries.Should().Equal(entryA, entryB);
            draw.Resolution.SlotResults.Should().Equal(
                new SlotDrawPlacement(entryA, "W1"),
                new SlotDrawPlacement(entryB, "W2"));
        }
    }

    [Fact]
    public async Task MatchPlacement_apply_round_tripsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var matchId = MatchId.New();
        var resourceId = ResourceId.New();
        var start = new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero);
        StageId stageId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Final", _clock);
            var fixture = stage.AddFixture(round.Id, _clock);
            stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
            stage.ApplyMatchPlacements(
                [new MatchPlacement(matchId, start, resourceId)],
                [matchId]);
            stageId = stage.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.MatchPlacements.Should().ContainSingle()
                .Which.Should().Be(new MatchPlacement(matchId, start, resourceId));
        }
    }
}
