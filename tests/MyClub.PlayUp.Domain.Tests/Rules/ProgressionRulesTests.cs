// -----------------------------------------------------------------------
// <copyright file="ProgressionRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class ProgressionRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Constructor_accepts_winner_and_loser_paths()
    {
        // Arrange
        var fixtureId = FixtureId.New();
        var stageId = StageId.New();
        var paths = new[]
        {
            new ProgressionPath(
                fixtureId,
                ProgressionOutcome.Winner,
                new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath(
                fixtureId,
                ProgressionOutcome.Loser,
                new ProgressionDestination(stageId, "Consolante-1"))
        };

        // Act
        var rules = new ProgressionRules(paths);

        // Assert
        rules.Paths.Should().HaveCount(2);
        rules.Paths.Should().Contain(p => p.Outcome == ProgressionOutcome.Winner && p.Destination.SlotKey == "SF1-A");
        rules.Paths.Should().Contain(p => p.Outcome == ProgressionOutcome.Loser && p.Destination.SlotKey == "Consolante-1");
    }

    [Fact]
    public void Constructor_rejects_empty_paths()
    {
        var act = () => new ProgressionRules([]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_destinations()
    {
        // Arrange
        var stageId = StageId.New();
        var paths = new[]
        {
            new ProgressionPath(FixtureId.New(), ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath(FixtureId.New(), ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A"))
        };

        // Act
        var act = () => new ProgressionRules(paths);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_fixture_outcome_sources()
    {
        // Arrange — same FixtureId + Outcome cannot feed two destinations
        var fixtureId = FixtureId.New();
        var stageId = StageId.New();
        var paths = new[]
        {
            new ProgressionPath(fixtureId, ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath(fixtureId, ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-B"))
        };

        // Act
        var act = () => new ProgressionRules(paths);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_when_direct_assignment_feeds_destination()
    {
        // Arrange
        var stage = StageAggregate.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.AssignEntryToSlot("SF1-A", EntryId.New(), _clock);

        var rules = new ProgressionRules(
        [
            new ProgressionPath(
                fixture.Id,
                ProgressionOutcome.Winner,
                new ProgressionDestination(stage.Id, "SF1-A"))
        ]);

        // Act
        var act = () => stage.ReplaceProgressionRules(rules, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotFeedConflict);
    }

    [Fact]
    public void Destination_rejects_empty_slot_key()
    {
        var act = () => new ProgressionDestination(StageId.New(), "   ");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void Path_has_no_order_property() => typeof(ProgressionPath).GetProperty("Order").Should().BeNull();

    [Fact]
    public void Copy_is_independent()
    {
        // Arrange
        var rules = new ProgressionRules(
        [
            new ProgressionPath(
                FixtureId.New(),
                ProgressionOutcome.Winner,
                new ProgressionDestination(StageId.New(), "Final-A"))
        ]);

        // Act
        var copy = rules.Copy();

        // Assert
        copy.Should().Be(rules);
        ReferenceEquals(copy.Paths[0], rules.Paths[0]).Should().BeFalse();
        ReferenceEquals(copy.Paths[0].Destination, rules.Paths[0].Destination).Should().BeFalse();
    }

    [Fact]
    public void StageRegulation_can_carry_optional_progression_rules()
    {
        // Arrange
        var progression = new ProgressionRules(
        [
            new ProgressionPath(
                FixtureId.New(),
                ProgressionOutcome.Winner,
                new ProgressionDestination(StageId.New(), "SF1-A"))
        ]);
        var baseReg = StageRegulation.MaterializeFrom(SampleRegulations.Standard());

        // Act
        var withProgression = baseReg.WithProgressionRules(progression);

        // Assert
        withProgression.ProgressionRules.Should().Be(progression);
        ReferenceEquals(withProgression.ProgressionRules, progression).Should().BeFalse();
        baseReg.ProgressionRules.Should().BeNull();
    }

    [Fact]
    public void ReplaceProgressionRules_accepts_fixture_of_stage_and_demotes_ready()
    {
        // Arrange
        var stage = StageAggregate.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.ClearDomainEvents();

        var rules = new ProgressionRules(
        [
            new ProgressionPath(
                fixture.Id,
                ProgressionOutcome.Winner,
                new ProgressionDestination(stage.Id, "SF1-A"))
        ]);

        // Act
        stage.ReplaceProgressionRules(rules, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.Regulation.ProgressionRules.Should().Be(rules);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRegulationReplaced>();
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_fixture_from_another_stage()
    {
        // Arrange
        var stage = StageAggregate.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);

        var rules = new ProgressionRules(
        [
            new ProgressionPath(
                FixtureId.New(),
                ProgressionOutcome.Winner,
                new ProgressionDestination(stage.Id, "SF1-A"))
        ]);

        // Act
        var act = () => stage.ReplaceProgressionRules(rules, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureNotFound);
    }
}
