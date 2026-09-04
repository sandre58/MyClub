// -----------------------------------------------------------------------
// <copyright file="StageStructureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
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
    public void AddMatchday_demotes_Ready_to_Draft_and_raises_StageMatchdayAdded()
    {
        // Arrange
        var stage = CreateReadyChampionship();
        stage.ClearDomainEvents();

        // Act
        stage.AddMatchday(2, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchdayAdded>();
    }

    [Fact]
    public void AddGroup_demotes_Ready_poules_and_raises_StageGroupAdded()
    {
        // Arrange
        var stage = CreateReadyPoules();
        stage.ClearDomainEvents();

        // Act
        var group = stage.AddGroup("B", _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        var added = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageGroupAdded>().Subject;
        added.GroupId.Should().Be(group.Id);
        added.OccurredOn.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void RemoveGroup_demotes_Ready_and_raises_StageGroupRemoved()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var a = stage.AddGroup("A", _clock);
        var b = stage.AddGroup("B", _clock);
        stage.AssignEntryToGroup(a.Id, EntryId.New());
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.RemoveGroup(b.Id, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageGroupRemoved>();
    }

    [Fact]
    public void RenameRound_real_change_keeps_Ready_without_event()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.RenameRound(round.Id, "Quarter-finals");

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        round.Name.Should().Be("Quarter-finals");
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RenameRound_same_name_is_noop()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.RenameRound(round.Id, "Final");

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMatchday_raises_StageMatchdayRemoved_and_demotes_Ready()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var first = stage.AddMatchday(1, _clock);
        stage.AddMatchday(2, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        // Act
        stage.RemoveMatchday(first.Id, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchdayRemoved>();
        stage.HasMatchday(first.Id).Should().BeFalse();
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
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();

        // Act
        var round = stage.AddRound("Final", _clock);

        // Assert
        var added = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRoundAdded>().Subject;
        added.RoundId.Should().Be(round.Id);
        added.StageId.Should().Be(stage.Id);
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
    public void RenameGroup_same_name_is_noop_and_keeps_Ready()
    {
        // Arrange
        var stage = CreateReadyPoules();
        var group = stage.Groups[0];
        stage.ClearDomainEvents();

        // Act
        stage.RenameGroup(group.Id, group.Name);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RenameGroup_real_change_keeps_Ready_without_event()
    {
        // Arrange
        var stage = CreateReadyPoules();
        var group = stage.Groups[0];
        stage.ClearDomainEvents();

        // Act
        stage.RenameGroup(group.Id, "Group Z");

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        group.Name.Should().Be("Group Z");
        stage.DomainEvents.Should().BeEmpty();
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
