// -----------------------------------------------------------------------
// <copyright file="CompetitionLifecycleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Competitions.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class CompetitionLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_starts_in_Draft_and_raises_CompetitionCreated()
    {
        // Arrange & Act
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.CompletionMode.Should().BeNull();
        competition.Regulation.Should().Be(SampleRegulations.Standard());
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionCreated>();
    }

    [Fact]
    public void Prepare_requires_active_entry_and_stage()
    {
        // Arrange
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Prepare_transitions_to_Ready_and_raises_CompetitionPrepared()
    {
        // Arrange
        var competition = CreatePreparedCandidate();
        competition.ClearDomainEvents();

        // Act
        competition.Prepare(_clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionPrepared>();
    }

    [Fact]
    public void Start_from_Ready_transitions_to_Running()
    {
        // Arrange
        var competition = CreateReady();
        competition.ClearDomainEvents();

        // Act
        competition.Start(_clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Running);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionStarted>();
    }

    [Fact]
    public void Start_from_Draft_is_rejected()
    {
        // Arrange
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.Start(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Suspend_Resume_roundtrip()
    {
        // Arrange
        var competition = CreateRunning();
        competition.ClearDomainEvents();

        // Act
        competition.Suspend(_clock);
        competition.Resume(_clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Running);
        competition.DomainEvents.Should().HaveCount(2);
        competition.DomainEvents[0].Should().BeOfType<CompetitionSuspended>();
        competition.DomainEvents[1].Should().BeOfType<CompetitionResumed>();
    }

    [Theory]
    [InlineData(CompletionMode.Normal)]
    [InlineData(CompletionMode.Administrative)]
    [InlineData(CompletionMode.Abandoned)]
    public void Complete_from_Running_sets_mode(CompletionMode mode)
    {
        // Arrange
        var competition = CreateRunning();
        competition.ClearDomainEvents();

        // Act
        competition.Complete(mode, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.CompletionMode.Should().Be(mode);
        competition.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CompetitionCompleted>()
            .Which.Mode.Should().Be(mode);
    }

    [Fact]
    public void Complete_from_Suspended_is_allowed()
    {
        // Arrange
        var competition = CreateRunning();
        competition.Suspend(_clock);
        competition.ClearDomainEvents();

        // Act
        competition.Complete(CompletionMode.Abandoned, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Completed);
    }

    [Fact]
    public void Archive_from_Completed_transitions_to_Archived()
    {
        // Arrange
        var competition = CreateRunning();
        competition.Complete(CompletionMode.Normal, _clock);
        competition.ClearDomainEvents();

        // Act
        competition.Archive(_clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Archived);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionArchived>();
    }

    [Fact]
    public void Archive_from_Running_is_rejected()
    {
        // Arrange
        var competition = CreateRunning();

        // Act
        var act = () => competition.Archive(_clock);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Prepare_from_Ready_is_rejected()
    {
        // Arrange
        var competition = CreateReady();

        // Act
        var act = () => competition.Prepare(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Suspend_from_Ready_is_rejected()
    {
        // Arrange
        var competition = CreateReady();

        // Act
        var act = () => competition.Suspend(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Resume_from_Running_is_rejected()
    {
        // Arrange
        var competition = CreateRunning();

        // Act
        var act = () => competition.Resume(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Complete_from_Draft_is_rejected()
    {
        // Arrange
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.Complete(CompletionMode.Normal, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Rename_on_Ready_keeps_Ready()
    {
        // Arrange
        var competition = CreateReady();
        competition.ClearDomainEvents();

        // Act
        competition.Rename(new CompetitionName("Renamed"), _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        competition.Name.Value.Should().Be("Renamed");
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionRenamed>();
    }

    [Fact]
    public void Rename_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateRunning();

        // Act
        var act = () => competition.Rename(new CompetitionName("X"), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private Competition CreatePreparedCandidate()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        return competition;
    }

    private Competition CreateReady()
    {
        var competition = CreatePreparedCandidate();
        competition.Prepare(_clock);
        return competition;
    }

    private Competition CreateRunning()
    {
        var competition = CreateReady();
        competition.Start(_clock);
        return competition;
    }
}
