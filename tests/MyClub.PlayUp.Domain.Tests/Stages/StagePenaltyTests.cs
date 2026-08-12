// -----------------------------------------------------------------------
// <copyright file="StagePenaltyTests.cs" company="Stéphane ANDRE">
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

public sealed class StagePenaltyTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AddPenalty_appends_to_collection_and_raises_rich_event()
    {
        var stage = CreateStage();
        var entry = EntryId.New();
        stage.ClearDomainEvents();

        var penalty = stage.AddPenalty(entry, 3, _clock, "  Misconduct  ");

        penalty.EntryId.Should().Be(entry);
        penalty.PointsDeducted.Should().Be(3);
        penalty.Reason.Should().Be("Misconduct");
        stage.Penalties.Should().ContainSingle().Which.Should().BeSameAs(penalty);
        var added = stage.DomainEvents.Should().ContainSingle(e => e is StagePenaltyAdded)
            .Which.Should().BeOfType<StagePenaltyAdded>().Subject;
        added.StageId.Should().Be(stage.Id);
        added.PenaltyId.Should().Be(penalty.Id);
        added.EntryId.Should().Be(entry);
        added.PointsDeducted.Should().Be(3);
        added.Reason.Should().Be("Misconduct");
    }

    [Fact]
    public void AddPenalty_trims_empty_reason_to_null()
    {
        var stage = CreateStage();

        var penalty = stage.AddPenalty(EntryId.New(), 1, _clock, "   ");

        penalty.Reason.Should().BeNull();
    }

    [Fact]
    public void AddPenalty_rejects_non_positive_points()
    {
        var stage = CreateStage();

        var act = () => stage.AddPenalty(EntryId.New(), 0, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.PenaltyInvalid);
        stage.Penalties.Should().BeEmpty();
    }

    [Fact]
    public void RemovePenalty_deletes_from_collection_and_raises_rich_event()
    {
        var stage = CreateStage();
        var entry = EntryId.New();
        var penalty = stage.AddPenalty(entry, 2, _clock, "X");
        stage.ClearDomainEvents();

        stage.RemovePenalty(penalty.Id, _clock);

        stage.Penalties.Should().BeEmpty();
        stage.FindPenalty(penalty.Id).Should().BeNull();
        var removed = stage.DomainEvents.Should().ContainSingle(e => e is StagePenaltyRemoved)
            .Which.Should().BeOfType<StagePenaltyRemoved>().Subject;
        removed.StageId.Should().Be(stage.Id);
        removed.PenaltyId.Should().Be(penalty.Id);
        removed.EntryId.Should().Be(entry);
        removed.PointsDeducted.Should().Be(2);
    }

    [Fact]
    public void RemovePenalty_rejects_unknown_id()
    {
        var stage = CreateStage();

        var act = () => stage.RemovePenalty(PenaltyId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.PenaltyNotFound);
    }

    [Fact]
    public void AddPenalty_allowed_while_running_or_suspended()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var running = stage.AddPenalty(EntryId.New(), 1, _clock);
        stage.Penalties.Should().Contain(running);

        stage.Suspend(_clock);
        var suspended = stage.AddPenalty(EntryId.New(), 2, _clock);
        stage.Penalties.Should().Contain(suspended);
    }

    [Fact]
    public void AddPenalty_and_RemovePenalty_reject_when_completed()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        var penalty = stage.AddPenalty(EntryId.New(), 1, _clock);
        stage.Complete(_clock);

        var add = () => stage.AddPenalty(EntryId.New(), 1, _clock);
        var remove = () => stage.RemovePenalty(penalty.Id, _clock);

        add.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        remove.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.Penalties.Should().ContainSingle();
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
}
