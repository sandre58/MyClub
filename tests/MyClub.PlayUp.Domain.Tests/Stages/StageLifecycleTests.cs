// -----------------------------------------------------------------------
// <copyright file="StageLifecycleTests.cs" company="Stéphane ANDRE">
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

public sealed class StageLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Create_starts_in_Draft_and_raises_StageCreated()
    {
        // Arrange & Act
        var stage = Stage.Create(_competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.CompetitionId.Should().Be(_competitionId);
        stage.Regulation.Should().NotBeNull();
        stage.Regulation.MatchRules.Should().Be(SampleRegulations.Standard().MatchRules);
        var created = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCreated>().Subject;
        created.CompetitionId.Should().Be(_competitionId);
        created.Name.Should().Be("Groups");
        created.OccurredOn.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_rejects_invalid_name()
    {
        // Arrange & Act
        var act = () => Stage.Create(_competitionId, new StageName(" "), SampleRegulations.Standard(), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NameInvalid);
    }

    [Fact]
    public void Prepare_without_structure_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Empty"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => stage.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NotReady);
    }

    [Fact]
    public void Prepare_championship_with_matchday_transitions_to_Ready()
    {
        // Arrange
        var stage = CreateChampionshipCandidate();
        stage.ClearDomainEvents();

        // Act
        stage.Prepare(_clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StagePrepared>();
    }

    [Fact]
    public void Prepare_elimination_with_one_round_is_ready()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Round of 16", _clock);
        stage.ClearDomainEvents();

        // Act
        stage.Prepare(_clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StagePrepared>();
    }

    [Fact]
    public void Prepare_poules_requires_group_with_entry_and_matchday()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AddMatchday(1, _clock);

        // Act — no entry yet
        var act = () => stage.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);

        // Arrange — assign entry
        stage.AssignEntryToGroup(group.Id, EntryId.New());
        stage.ClearDomainEvents();

        // Act
        stage.Prepare(_clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Prepare_poules_without_matchday_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.AssignEntryToGroup(group.Id, EntryId.New());

        // Act
        var act = () => stage.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void Start_from_Ready_transitions_to_Running()
    {
        // Arrange
        var stage = CreateReady();
        stage.ClearDomainEvents();

        // Act
        stage.Start(_clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Running);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageStarted>();
    }

    [Fact]
    public void Start_from_Draft_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("X"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => stage.Start(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Suspend_Resume_roundtrip()
    {
        // Arrange
        var stage = CreateRunning();
        stage.ClearDomainEvents();

        // Act
        stage.Suspend(_clock);
        stage.Resume(_clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Running);
        stage.DomainEvents.Should().HaveCount(2);
        stage.DomainEvents[0].Should().BeOfType<StageSuspended>();
        stage.DomainEvents[1].Should().BeOfType<StageResumed>();
    }

    [Fact]
    public void Suspend_from_Ready_is_rejected()
    {
        // Arrange
        var stage = CreateReady();

        // Act
        var act = () => stage.Suspend(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Resume_from_Running_is_rejected()
    {
        // Arrange
        var stage = CreateRunning();

        // Act
        var act = () => stage.Resume(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Complete_from_Running_and_from_Suspended()
    {
        // Arrange
        var running = CreateRunning();
        running.ClearDomainEvents();
        var suspended = CreateRunning();
        suspended.Suspend(_clock);
        suspended.ClearDomainEvents();

        // Act
        running.Complete(_clock);
        suspended.Complete(_clock);

        // Assert
        running.Status.Should().Be(StageStatus.Completed);
        suspended.Status.Should().Be(StageStatus.Completed);
        running.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCompleted>();
        suspended.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCompleted>();
    }

    [Fact]
    public void Complete_from_Draft_is_rejected()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("X"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => stage.Complete(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Rename_same_name_is_noop()
    {
        // Arrange
        var stage = Stage.Create(_competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        stage.ClearDomainEvents();

        // Act
        stage.Rename(new StageName("Groups"));

        // Assert
        stage.DomainEvents.Should().BeEmpty();
        stage.Name.Value.Should().Be("Groups");
    }

    [Fact]
    public void Rename_real_change_keeps_Ready()
    {
        // Arrange
        var stage = CreateReady();
        stage.ClearDomainEvents();

        // Act
        stage.Rename(new StageName("Renamed"));

        // Assert
        stage.Status.Should().Be(StageStatus.Ready);
        stage.Name.Value.Should().Be("Renamed");
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Structural_add_after_Start_is_rejected()
    {
        // Arrange
        var stage = CreateRunning();

        // Act
        var act = () => stage.AddMatchday(2, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void Prepare_from_Ready_is_rejected()
    {
        // Arrange
        var stage = CreateReady();

        // Act
        var act = () => stage.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Rename_after_Start_is_allowed()
    {
        // Arrange
        var stage = CreateRunning();

        // Act
        stage.Rename(new StageName("Later"));

        // Assert
        stage.Name.Value.Should().Be("Later");
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Structure_mutation_when_Suspended_or_Completed_is_rejected()
    {
        // Arrange
        var suspended = CreateRunning();
        suspended.Suspend(_clock);
        var completed = CreateRunning();
        completed.Complete(_clock);

        // Act & Assert
        ((Action)(() => suspended.AddMatchday(2, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        ((Action)(() => completed.RemoveMatchday(completed.Matchdays[0].Id, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void CompetitionId_is_immutable_after_Create()
    {
        // Arrange & Act
        var stage = Stage.Create(_competitionId, new StageName("X"), SampleRegulations.Standard(), _clock);

        // Assert — no public setter; identity fixed at creation
        stage.CompetitionId.Should().Be(_competitionId);
        typeof(Stage).GetProperty(nameof(Stage.CompetitionId))!.SetMethod.Should().BeNull();
    }

    private Stage CreateChampionshipCandidate()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        return stage;
    }

    private Stage CreateReady()
    {
        var stage = CreateChampionshipCandidate();
        stage.Prepare(_clock);
        return stage;
    }

    private Stage CreateRunning()
    {
        var stage = CreateReady();
        stage.Start(_clock);
        return stage;
    }
}
