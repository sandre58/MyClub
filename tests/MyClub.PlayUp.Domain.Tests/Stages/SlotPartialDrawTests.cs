// -----------------------------------------------------------------------
// <copyright file="SlotPartialDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class SlotPartialDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Compose_keeps_manual_and_resolved_occupants_out_of_the_free_pool()
    {
        var stage = CreateCup(4);
        var entries = AddPopulation(stage, 4);
        stage.AssignEntryToSlot("S1", entries[0]);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);

        var scope = SlotPartialDraw.Compose(stage, EmptySnapshot(stage));

        scope.IsDrawable.Should().BeTrue();
        scope.VacantSlotKeys.Should().Equal("S3", "S4");
        scope.UnplacedEntries.Should().Equal(entries[2], entries[3]);
    }

    [Fact]
    public void Compose_refuses_when_every_slot_is_occupied()
    {
        var stage = CreateCup(2);
        var entries = AddPopulation(stage, 2);
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);

        var scope = SlotPartialDraw.Compose(stage, EmptySnapshot(stage));

        scope.Refusal.Should().Be(SlotPartialDrawRefusal.NoFreeSlot);
        scope.VacantSlotKeys.Should().BeEmpty();
        scope.UnplacedEntries.Should().BeEmpty();
    }

    [Fact]
    public void Compose_refuses_when_unplaced_teams_do_not_fill_free_slots()
    {
        var stage = CreateCup(4);
        var entries = AddPopulation(stage, 2);
        stage.ApplyResolvedEntry("S1", entries[0], _clock);

        var scope = SlotPartialDraw.Compose(stage, EmptySnapshot(stage));

        scope.Refusal.Should().Be(SlotPartialDrawRefusal.NotEnoughTeams);
        scope.VacantSlotKeys.Should().Equal("S2", "S3", "S4");
        scope.UnplacedEntries.Should().Equal(entries[1]);
    }

    [Fact]
    public void Compose_refuses_when_more_unplaced_teams_than_free_slots()
    {
        var stage = CreateCup(2);
        var entries = AddPopulation(stage, 3);
        stage.ApplyResolvedEntry("S1", entries[0], _clock);

        var scope = SlotPartialDraw.Compose(stage, EmptySnapshot(stage));

        scope.Refusal.Should().Be(SlotPartialDrawRefusal.TooManyTeams);
        scope.VacantSlotKeys.Should().Equal("S2");
        scope.UnplacedEntries.Should().Equal(entries[1], entries[2]);
    }

    [Fact]
    public void Compose_does_not_treat_a_reserved_feed_as_a_free_slot()
    {
        var stage = CreateCup(2);
        var entries = AddPopulation(stage, 2);
        var snapshot = new SlotFeedSnapshot(
            stage.Id,
            ["S1", "S2"],
            [],
            [new QualificationFeedSource(StageId.New(), 1, "S1")],
            [],
            []);

        var scope = SlotPartialDraw.Compose(stage, snapshot);

        scope.Refusal.Should().Be(SlotPartialDrawRefusal.TooManyTeams);
        scope.VacantSlotKeys.Should().Equal("S2");
        scope.UnplacedEntries.Should().Equal(entries[0], entries[1]);
    }

    [Fact]
    public void Compose_does_not_treat_a_direct_assignment_feed_as_a_free_slot()
    {
        var stage = CreateCup(2);
        var entries = AddPopulation(stage, 2);
        var snapshot = new SlotFeedSnapshot(
            stage.Id,
            ["S1", "S2"],
            [new DirectFeedSource("S1", entries[0])],
            [],
            [],
            []);

        var scope = SlotPartialDraw.Compose(stage, snapshot);

        scope.Refusal.Should().Be(SlotPartialDrawRefusal.TooManyTeams);
        scope.VacantSlotKeys.Should().Equal("S2");
        scope.UnplacedEntries.Should().Equal(entries[0], entries[1]);
    }

    [Fact]
    public void Generator_draws_only_the_free_subset_and_omits_preserved_slots()
    {
        var entries = new[] { EntryId.New(), EntryId.New(), EntryId.New(), EntryId.New() };
        var request = new DrawGenerationRequest(
            DrawResolutionKind.Slot,
            [entries[2], entries[3]],
            [],
            DrawConstraintContext.Empty,
            new ZeroRandom(),
            targets: ["S3", "S4"]);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        result.Resolution!.SlotResults.Select(placement => placement.SlotKey).Should().Equal("S3", "S4");
        result.Resolution.SlotResults.Select(placement => placement.EntryId)
            .Should().BeEquivalentTo([entries[2], entries[3]]);
        result.Resolution.SlotResults.Should().NotContain(placement =>
            placement.EntryId.Equals(entries[0]) || placement.EntryId.Equals(entries[1]));
    }

    private static SlotFeedSnapshot EmptySnapshot(Stage stage) =>
        new(
            stage.Id,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            [],
            [],
            [],
            []);

    private Stage CreateCup(int slotCount)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        for (var index = 1; index <= slotCount; index++)
        {
            stage.AddSlot($"S{index}");
        }

        return stage;
    }

    private EntryId[] AddPopulation(Stage stage, int count)
    {
        var entries = Enumerable.Range(0, count).Select(_ => EntryId.New()).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        return entries;
    }

    private sealed class ZeroRandom : IRandomSource
    {
        public int NextInt32(int minInclusive, int maxExclusive) => minInclusive;

        public double NextDouble() => 0;

        public void NextBytes(byte[] buffer) => Array.Clear(buffer);
    }
}
