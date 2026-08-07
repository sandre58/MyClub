// -----------------------------------------------------------------------
// <copyright file="CompetitionStagesTests.cs" company="Stéphane ANDRE">
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

public sealed class CompetitionStagesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddStage_on_Ready_demotes_to_Draft()
    {
        // Arrange
        var competition = CreateReady();
        competition.ClearDomainEvents();
        var stageId = StageId.New();

        // Act
        competition.AddStage(stageId, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.HasStage(stageId).Should().BeTrue();
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionStageAdded>();
    }

    [Fact]
    public void AddStage_rejects_duplicate()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var stageId = StageId.New();
        competition.AddStage(stageId, _clock);

        // Act
        var act = () => competition.AddStage(stageId, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.DuplicateStage);
    }

    [Fact]
    public void RemoveStage_on_Ready_demotes_to_Draft()
    {
        // Arrange
        var competition = CreateReady();
        var stageId = competition.StageIds[0];
        competition.ClearDomainEvents();

        // Act
        competition.RemoveStage(stageId, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.HasStage(stageId).Should().BeFalse();
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionStageRemoved>();
    }

    [Fact]
    public void RemoveStage_missing_throws()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);

        // Act
        var act = () => competition.RemoveStage(StageId.New(), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.StageNotFound);
    }

    [Fact]
    public void SetStageOrder_permutation_demotes_Ready_and_raises_event()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        var first = StageId.New();
        var second = StageId.New();
        competition.AddStage(first, _clock);
        competition.AddStage(second, _clock);
        competition.Prepare(_clock);
        competition.ClearDomainEvents();

        // Act
        competition.SetStageOrder([second, first], _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.StageIds.Should().Equal(second, first);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionStageOrderChanged>();
    }

    [Fact]
    public void SetStageOrder_rejects_non_permutation()
    {
        // Arrange
        var competition = CreateReady();
        var existing = competition.StageIds[0];

        // Act
        var act = () => competition.SetStageOrder([existing, StageId.New()], _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidStageOrder);
        competition.Status.Should().Be(CompetitionStatus.Ready);
    }

    [Fact]
    public void SetStageOrder_same_order_is_noop()
    {
        // Arrange
        var competition = CreateReady();
        var order = competition.StageIds.ToList();
        competition.ClearDomainEvents();

        // Act
        competition.SetStageOrder(order, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Ready);
        competition.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetStageOrder_event_payload_is_immutable_snapshot()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        var first = StageId.New();
        var second = StageId.New();
        competition.AddStage(first, _clock);
        competition.AddStage(second, _clock);
        competition.Prepare(_clock);
        competition.ClearDomainEvents();

        // Act
        competition.SetStageOrder([second, first], _clock);
        var payload = competition.DomainEvents.OfType<CompetitionStageOrderChanged>().Single().StageIds;
        competition.AddStage(StageId.New(), _clock);

        // Assert
        payload.Should().Equal(second, first);
        payload.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveStage_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateReady();
        competition.Start(_clock);
        var stageId = competition.StageIds[0];

        // Act
        var act = () => competition.RemoveStage(stageId, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void AddStage_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateReady();
        competition.Start(_clock);

        // Act
        var act = () => competition.AddStage(StageId.New(), _clock);

        // Assert
        act.Should().Throw<DomainException>();
    }

    private CompetitionAggregate CreateReady()
    {
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        return competition;
    }
}
