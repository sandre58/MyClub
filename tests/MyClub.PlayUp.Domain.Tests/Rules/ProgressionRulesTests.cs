// -----------------------------------------------------------------------
// <copyright file="ProgressionRulesTests.cs" company="Stéphane ANDRE">
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

public sealed class ProgressionRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Constructor_accepts_winner_and_loser_paths()
    {
        // Arrange
        FixtureId.New();
        var stageId = StageId.New();
        var paths = new[]
        {
            new ProgressionPath("P1",
                ProgressionOutcome.Winner,
                new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath("P1",
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
            new ProgressionPath("P1", ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath("P1", ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A"))
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
        FixtureId.New();
        var stageId = StageId.New();
        var paths = new[]
        {
            new ProgressionPath("P1", ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-A")),
            new ProgressionPath("P1", ProgressionOutcome.Winner, new ProgressionDestination(stageId, "SF1-B"))
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
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        stage.AssignEntryToSlot("SF1-A", Admit(stage, EntryId.New()));

        var rules = new ProgressionRules(
        [
            new ProgressionPath("P1",
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
            new ProgressionPath("P1",
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
            new ProgressionPath("P1",
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
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.ClearDomainEvents();

        var rules = new ProgressionRules(
        [
            new ProgressionPath("P1",
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
    public void ReplaceProgressionRules_allows_cross_stage_place_when_slot_on_destination()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("QF"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        var peerId = StageId.New();

        var act = () => stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(peerId, "SF1-A"))
            ]),
            _clock);

        act.Should().NotThrow();
        stage.Regulation.ProgressionRules!.Paths.Should().ContainSingle()
            .Which.Destination.SlotKey.Should().Be("SF1-A");
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_fixture_from_another_stage()
    {
        // Arrange
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);

        var rules = new ProgressionRules(
        [
            new ProgressionPath("P1",
                ProgressionOutcome.Winner,
                new ProgressionDestination(stage.Id, "SF1-A"))
        ]);

        // Act
        var act = () => stage.ReplaceProgressionRules(rules, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    private EntryId Admit(Stage stage, EntryId entryId)
    {
        var existing = stage.CompositionEntries.Select(e => e.EntryId).ToList();
        existing.Add(entryId);
        stage.ReplaceAffectationAuthoring(existing, _clock);
        return entryId;
    }
}
