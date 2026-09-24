// -----------------------------------------------------------------------
// <copyright file="BracketPairTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class BracketPairTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CreateEntryRoundPairs_maps_adjacent_slots_to_persistent_P_keys()
    {
        var pairs = BracketPair.CreateEntryRoundPairs(["S1", "S2", "S3", "S4"]);

        pairs.Should().Equal(
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4"));
    }

    [Fact]
    public void BracketPair_rejects_identical_slots()
    {
        var act = () => new BracketPair("P1", "S1", "S1");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void SeedEntryRoundBracketPairs_on_cup_stage_covers_all_slots()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Tour principal", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.AddSlot("S3");
        stage.AddSlot("S4");

        stage.SeedEntryRoundBracketPairs();

        stage.BracketPairs.Should().HaveCount(2);
        stage.FindBracketPair("P1")!.SlotAKey.Should().Be("S1");
        stage.FindBracketPair("P2")!.SlotBKey.Should().Be("S4");
    }

    [Fact]
    public void ReplaceBracketPairs_rejects_slot_in_two_pairs()
    {
        var stage = CreateSeededCup();

        var act = () => stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S2", "S3")
        ]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void MatchesSlots_accepts_swapped_A_B()
    {
        var pair = new BracketPair("P1", "S1", "S2");

        pair.MatchesSlots("S2", "S1").Should().BeTrue();
        pair.MatchesSlots("S1", "S3").Should().BeFalse();
    }

    [Fact]
    public void AddFixture_with_pair_key_binds_when_slots_match()
    {
        var stage = CreateSeededCup();

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        fixture.BracketPairKey.Should().Be("P1");
        fixture.SlotAKey.Should().Be("S1");
        fixture.SlotBKey.Should().Be("S2");
    }

    [Fact]
    public void AddFixture_accepts_swapped_slots_for_pair()
    {
        var stage = CreateSeededCup();

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "S2", "S1", "P1");

        fixture.BracketPairKey.Should().Be("P1");
        stage.FindBracketPair("P1")!.MatchesSlots(fixture.SlotAKey, fixture.SlotBKey).Should().BeTrue();
    }

    [Fact]
    public void AddFixture_rejects_unknown_pair_key()
    {
        var stage = CreateSeededCup();

        var act = () => stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P999");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void AddFixture_rejects_slots_that_do_not_match_pair()
    {
        var stage = CreateSeededCup();

        var act = () => stage.AddFixture(stage.Rounds[0].Id, _clock, "S3", "S4", "P1");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void AddFixture_rejects_second_fixture_for_same_pair_key()
    {
        var stage = CreateSeededCup();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        var act = () => stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void AddFixture_rejects_missing_pair_key_when_stage_has_bracket_pairs()
    {
        var stage = CreateSeededCup();

        var act = () => stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void ClearBracketPairs_rejects_when_fixture_references_pair()
    {
        var stage = CreateSeededCup();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        var act = () => stage.ClearBracketPairs();

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
        stage.BracketPairs.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceBracketPairs_rejects_removing_referenced_pair()
    {
        var stage = CreateSeededCup();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        var act = () => stage.ReplaceBracketPairs([new BracketPair("P2", "S3", "S4")]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void ReplaceBracketPairs_allows_keeping_referenced_pair()
    {
        var stage = CreateSeededCup();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");

        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4")
        ]);

        stage.BracketPairs.Should().HaveCount(2);
    }

    private Stage CreateSeededCup()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Tour principal", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.AddSlot("S3");
        stage.AddSlot("S4");
        stage.SeedEntryRoundBracketPairs();
        return stage;
    }
}
