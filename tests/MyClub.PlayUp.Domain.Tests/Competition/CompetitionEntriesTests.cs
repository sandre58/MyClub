// -----------------------------------------------------------------------
// <copyright file="CompetitionEntriesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competition;
using MyClub.PlayUp.Domain.Competition.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using CompetitionAggregate = MyClub.PlayUp.Domain.Competition.Competition;

namespace MyClub.PlayUp.Domain.Tests.Competition;

public sealed class CompetitionEntriesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddEntry_on_Ready_demotes_to_Draft()
    {
        // Arrange
        var competition = CreateReady();
        competition.ClearDomainEvents();
        var teamId = TeamId.New();

        // Act
        var entry = competition.AddEntry(teamId, "Team B", _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        entry.Status.Should().Be(EntryStatus.Active);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionEntryAdded>();
    }

    [Fact]
    public void AddEntry_rejects_duplicate_occupying_team()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var teamId = TeamId.New();
        competition.AddEntry(teamId, "Team A", _clock);

        // Act
        var act = () => competition.AddEntry(teamId, "Team A again", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.DuplicateTeam);
    }

    [Fact]
    public void AddEntry_allows_reentry_after_Withdraw()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var teamId = TeamId.New();
        var first = competition.AddEntry(teamId, "Team A", _clock);
        competition.WithdrawEntry(first.Id, _clock);

        // Act
        var second = competition.AddEntry(teamId, "Team A return", _clock);

        // Assert
        second.Id.Should().NotBe(first.Id);
        competition.ContainsTeam(teamId).Should().BeTrue();
        competition.FindEntry(teamId)!.Id.Should().Be(second.Id);
    }

    [Fact]
    public void RenameEntry_on_Ready_keeps_Ready()
    {
        // Arrange
        var competition = CreateReady();
        var entry = competition.Entries[0];
        competition.ClearDomainEvents();

        // Act
        competition.RenameEntry(entry.Id, "New Name", _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        entry.DisplayName.Should().Be("New Name");
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionEntryRenamed>();
    }

    [Fact]
    public void WithdrawEntry_on_Ready_keeps_Ready()
    {
        // Arrange
        var competition = CreateReady();
        var entry = competition.Entries[0];
        competition.ClearDomainEvents();

        // Act
        competition.WithdrawEntry(entry.Id, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        entry.Status.Should().Be(EntryStatus.Withdrawn);
        competition.ContainsTeam(entry.TeamId).Should().BeFalse();
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionEntryWithdrawn>();
    }

    [Fact]
    public void WithdrawEntry_is_allowed_while_Running()
    {
        // Arrange
        var competition = CreateRunning();
        var entry = competition.Entries[0];

        // Act
        competition.WithdrawEntry(entry.Id, _clock);

        // Assert
        entry.Status.Should().Be(EntryStatus.Withdrawn);
    }

    [Fact]
    public void ExcludeEntry_is_rejected_while_Running()
    {
        // Arrange
        var competition = CreateRunning();
        var entry = competition.Entries[0];

        // Act
        var act = () => competition.ExcludeEntry(entry.Id, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void ExcludeEntry_on_Draft_sets_Excluded()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.ClearDomainEvents();

        // Act
        competition.ExcludeEntry(entry.Id, _clock);

        // Assert
        entry.Status.Should().Be(EntryStatus.Excluded);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionEntryExcluded>();
    }

    [Fact]
    public void AddEntry_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateRunning();

        // Act
        var act = () => competition.AddEntry(TeamId.New(), "Late", _clock);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ExcludeEntry_on_Ready_keeps_Ready()
    {
        // Arrange
        var competition = CreateReady();
        var entry = competition.Entries[0];
        competition.ClearDomainEvents();

        // Act
        competition.ExcludeEntry(entry.Id, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        entry.Status.Should().Be(EntryStatus.Excluded);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionEntryExcluded>();
    }

    [Fact]
    public void AddEntry_allows_reentry_after_Exclude()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var teamId = TeamId.New();
        var first = competition.AddEntry(teamId, "Team A", _clock);
        competition.ExcludeEntry(first.Id, _clock);

        // Act
        var second = competition.AddEntry(teamId, "Team A return", _clock);

        // Assert
        second.Id.Should().NotBe(first.Id);
        competition.FindEntry(teamId)!.Id.Should().Be(second.Id);
    }

    [Fact]
    public void WithdrawEntry_rejects_non_active_entry()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.WithdrawEntry(entry.Id, _clock);

        // Act
        var act = () => competition.WithdrawEntry(entry.Id, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void AddEntry_rejects_invalid_display_name()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.AddEntry(TeamId.New(), "   ", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidDisplayName);
    }

    [Fact]
    public void RenameEntry_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateRunning();
        var entry = competition.Entries[0];

        // Act
        var act = () => competition.RenameEntry(entry.Id, "X", _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void GetEntry_throws_when_missing()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.GetEntry(EntryId.New());

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.EntryNotFound);
    }

    private CompetitionAggregate CreateReady()
    {
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        return competition;
    }

    private CompetitionAggregate CreateRunning()
    {
        var competition = CreateReady();
        competition.Start(_clock);
        return competition;
    }
}
