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

    [Fact]
    public void Constructor_accepts_winner_and_loser_ranks_for_same_fixture()
    {
        var fixtureId = FixtureId.New();
        var paths = new[]
        {
            new PlacementAwardPath(fixtureId, ProgressionOutcome.Winner, rank: 3),
            new PlacementAwardPath(fixtureId, ProgressionOutcome.Loser, rank: 4)
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
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 1)
        };

        var act = () => new PlacementAwardRules(paths);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_fixture_outcome_sources()
    {
        var fixtureId = FixtureId.New();
        var paths = new[]
        {
            new PlacementAwardPath(fixtureId, ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(fixtureId, ProgressionOutcome.Winner, rank: 2)
        };

        var act = () => new PlacementAwardRules(paths);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Path_rejects_rank_below_one()
    {
        var act = () => new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PlacementAwardRulesInvalid);
    }

    [Fact]
    public void Copy_is_independent()
    {
        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 1)
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
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Loser, rank: 2)
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
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.ClearDomainEvents();

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(fixture.Id, ProgressionOutcome.Winner, rank: 1),
            new PlacementAwardPath(fixture.Id, ProgressionOutcome.Loser, rank: 2)
        ]);

        stage.ReplacePlacementAwardRules(rules, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.Regulation.PlacementAwardRules.Should().Be(rules);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRegulationReplaced>();
    }

    [Fact]
    public void ReplacePlacementAwardRules_rejects_fixture_from_another_stage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Final", _clock);

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(FixtureId.New(), ProgressionOutcome.Winner, rank: 1)
        ]);

        var act = () => stage.ReplacePlacementAwardRules(rules, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureNotFound);
    }

    [Fact]
    public void ReplacePlacementAwardRules_does_not_require_slots()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Bronze"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("3rd place", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);

        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(fixture.Id, ProgressionOutcome.Winner, rank: 3),
            new PlacementAwardPath(fixture.Id, ProgressionOutcome.Loser, rank: 4)
        ]);

        stage.ReplacePlacementAwardRules(rules, _clock);

        stage.Regulation.PlacementAwardRules.Should().Be(rules);
        stage.Slots.Should().BeEmpty();
    }
}
