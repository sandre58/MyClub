// -----------------------------------------------------------------------
// <copyright file="StageStructureTests.cs" company="Stéphane ANDRE">
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

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageStructureTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AddMatchday_in_Ready_is_rejected()
    {
        var stage = CreateReadyChampionship();
        stage.ClearDomainEvents();

        var act = () => stage.AddMatchday(2, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddGroup_in_Ready_is_rejected()
    {
        var stage = CreateReadyPoules();
        stage.ClearDomainEvents();

        var act = () => stage.AddGroup("B", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RemoveGroup_in_Ready_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);
        var b = stage.AddGroup("B", _clock);
        stage.AssignEntryToGroup(a.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        var act = () => stage.RemoveGroup(b.Id, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RenameRound_in_Ready_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("QF", _clock);
        stage.Prepare(_clock);

        var act = () => stage.RenameRound(round.Id, "Quarter-finals");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void RenameRound_same_name_is_noop_in_Draft()
    {
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        stage.ClearDomainEvents();

        stage.RenameRound(round.Id, "Final");

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMatchday_in_Ready_is_rejected()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var first = stage.AddMatchday(1, _clock);
        stage.AddMatchday(2, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        var act = () => stage.RemoveMatchday(first.Id, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.HasMatchday(first.Id).Should().BeTrue();
    }

    [Fact]
    public void AddGroup_with_rounds_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("QF", _clock);

        // Act
        var act = () => stage.AddGroup("A", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddRound_with_matchdays_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Mixed"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);

        // Act
        var act = () => stage.AddRound("Final", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddRound_with_groups_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Mixed"), SampleRegulations.Standard(), _clock);
        stage.AddGroup("A", _clock);

        // Act
        var act = () => stage.AddRound("Final", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddMatchday_with_rounds_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("SF", _clock);

        // Act
        var act = () => stage.AddMatchday(1, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void RemoveGroup_raises_StageGroupRemoved()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.ClearDomainEvents();

        // Act
        stage.RemoveGroup(group.Id, _clock);

        // Assert
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageGroupRemoved>();
        stage.HasGroup(group.Id).Should().BeFalse();
    }

    [Fact]
    public void RemoveRound_raises_StageRoundRemoved()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("QF", _clock);
        stage.ClearDomainEvents();

        // Act
        stage.RemoveRound(round.Id, _clock);

        // Assert
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRoundRemoved>();
        stage.HasRound(round.Id).Should().BeFalse();
    }

    [Fact]
    public void AddRound_raises_StageRoundAdded()
    {
        // Arrange — Cup seed without standing so AddRound does not also clear StandingRules
        var stage = Stage.Create(
            _competitionId,
            new StageName("Cup"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: false),
            _clock);
        stage.ClearDomainEvents();

        // Act
        var round = stage.AddRound("Final", _clock);

        // Assert
        var added = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRoundAdded>().Subject;
        added.RoundId.Should().Be(round.Id);
        added.StageId.Should().Be(stage.Id);
    }

    [Fact]
    public void AddRound_clears_standing_rules_when_becoming_knockout()
    {
        var stage = Stage.Create(
            _competitionId,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.Regulation.StandingRules.Should().NotBeNull();

        stage.AddRound("Final", _clock);

        stage.Regulation.StandingRules.Should().BeNull();
        StageClassification.IsNonClassifyingPhase(stage).Should().BeTrue();
    }

    [Fact]
    public void AddGroup_raises_StageGroupAdded()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();

        // Act
        var group = stage.AddGroup("B", _clock);

        // Assert
        var added = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageGroupAdded>().Subject;
        added.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public void RenameGroup_same_name_is_noop_in_Draft()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.ClearDomainEvents();

        stage.RenameGroup(group.Id, group.Name);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RenameGroup_in_Ready_is_rejected()
    {
        var stage = CreateReadyPoules();
        var group = stage.Groups[0];

        var act = () => stage.RenameGroup(group.Id, "Group Z");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    private Stage CreateReadyChampionship()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }

    private Stage CreateReadyPoules()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }
}
