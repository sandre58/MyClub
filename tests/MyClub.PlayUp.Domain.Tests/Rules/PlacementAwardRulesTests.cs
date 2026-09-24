// -----------------------------------------------------------------------
// <copyright file="PlacementAwardRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class PlacementAwardRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 10, 0, 0, TimeSpan.Zero));

    private static string PathKey(Fixture fixture) =>
        fixture.BracketPairKey ?? throw new InvalidOperationException("missing BracketPairKey");

    [Fact]
    public void Constructor_accepts_winner_and_loser_ranks_for_same_source()
    {
        var sourcePairKey = "P1";
        var paths = new[]
        {
            new PlacementAwardPath(sourcePairKey, ProgressionOutcome.Winner, rank: 3),
            new PlacementAwardPath(sourcePairKey, ProgressionOutcome.Loser, rank: 4)
        };

        var rules = new PlacementAwardRules(paths);

        rules.Paths.Should().HaveCount(2);
        rules.Paths.Should().Contain(p => p.Outcome == ProgressionOutcome.Winner && p.Rank == 3);
        rules.Paths.Should().Contain(p => p.Outcome == ProgressionOutcome.Loser && p.Rank == 4);
    }

    [Fact]
    public void Constructor_rejects_empty_paths()
    {
        var act = () => new PlacementAwardRules([]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_ranks()
    {
        var paths = new[]
        {
            new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)
        };

        var act = () => new PlacementAwardRules(paths);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_source_outcome()
    {
        var sourcePairKey = "P1";
        var paths = new[]
        {
            new PlacementAwardPath(sourcePairKey, ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(sourcePairKey, ProgressionOutcome.Winner, rank: 2)
        };

        var act = () => new PlacementAwardRules(paths);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Path_rejects_rank_below_one()
    {
        var act = () => new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Copy_is_independent()
    {
        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)
        ]);

        var copy = rules.Copy();

        copy.Should().Be(rules);
        ReferenceEquals(copy.Paths[0], rules.Paths[0]).Should().BeFalse();
    }

    [Fact]
    public void StageRegulation_can_carry_optional_placement_award_rules()
    {
        var awards = new PlacementAwardRules(
        [
            new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath("P1", ProgressionOutcome.Loser, rank: 2)
        ]);
        var baseReg = StageRegulation.MaterializeFrom(SampleRegulations.Standard());

        var withAwards = baseReg.WithPlacementAwardRules(awards);

        withAwards.PlacementAwardRules.Should().Be(awards);
        ReferenceEquals(withAwards.PlacementAwardRules, awards).Should().BeFalse();
        baseReg.PlacementAwardRules.Should().BeNull();
        withAwards.ProgressionRules.Should().BeNull();
    }

    [Fact]
    public void ReplacePlacementAwardRules_accepts_fixture_of_stage_and_demotes_ready()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("Final", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var fixture = stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.ClearDomainEvents();

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Loser, rank: 2)
        ]);

        stage.ReplacePlacementAwardRules(rules, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.Regulation.PlacementAwardRules.Should().Be(rules);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRegulationReplaced>();
    }

    [Fact]
    public void ReplacePlacementAwardRules_accepts_PairKey_on_BracketPairs_without_fixtures()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Tour principal", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.AddSlot("S3");
        stage.AddSlot("S4");
        stage.SeedEntryRoundBracketPairs();
        stage.BracketPairs.Should().HaveCount(2);
        stage.Rounds[0].Fixtures.Should().BeEmpty();

        var pairKey = stage.BracketPairs[0].PairKey;
        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(pairKey, ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(pairKey, ProgressionOutcome.Loser, rank: 2)
        ]);

        stage.ReplacePlacementAwardRules(rules, _clock);

        stage.Regulation.PlacementAwardRules.Should().Be(rules);
        stage.Regulation.PlacementAwardRules!.Paths.Select(p => p.SourcePairKey).Should().Equal(pairKey, pairKey);
        stage.Rounds[0].Fixtures.Should().BeEmpty();
    }

    [Fact]
    public void ReplacePlacementAwardRules_rejects_unknown_source()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Final", _clock);

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)
        ]);

        var act = () => stage.ReplacePlacementAwardRules(rules, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void ReplacePlacementAwardRules_does_not_require_destination_place_slots()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Bronze"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("3rd place", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var fixture = stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Winner, rank: 3),
            new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Loser, rank: 4)
        ]);

        stage.ReplacePlacementAwardRules(rules, _clock);

        stage.Regulation.PlacementAwardRules.Should().Be(rules);
        stage.DirectAssignments.Should().BeEmpty();
    }
}
