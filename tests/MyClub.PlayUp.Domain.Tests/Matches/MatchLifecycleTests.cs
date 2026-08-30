// -----------------------------------------------------------------------
// <copyright file="MatchLifecycleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class MatchLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 15, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    [Fact]
    public void Create_starts_Scheduled_and_raises_MatchCreated()
    {
        // Arrange & Act
        var match = Match.Create(_competitionId, _stageId, _home, _away, _clock);

        // Assert
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.Result.Should().BeNull();
        match.CompetitionId.Should().Be(_competitionId);
        match.StageId.Should().Be(_stageId);
        match.HomeEntryId.Should().Be(_home);
        match.AwayEntryId.Should().Be(_away);

        var created = match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchCreated>().Subject;
        created.MatchId.Should().Be(match.Id);
        created.CompetitionId.Should().Be(_competitionId);
        created.StageId.Should().Be(_stageId);
        created.HomeEntryId.Should().Be(_home);
        created.AwayEntryId.Should().Be(_away);
        created.OccurredOn.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_rejects_same_home_and_away()
    {
        // Arrange
        var entry = EntryId.New();

        // Act
        var act = () => Match.Create(_competitionId, _stageId, entry, entry, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SameParticipant);
    }

    [Fact]
    public void Create_rejects_empty_competition_id()
    {
        // Arrange & Act
        var act = () => Match.Create(new CompetitionId(Guid.Empty), _stageId, _home, _away, _clock);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Identifiers_remain_unchanged_after_lifecycle_mutations()
    {
        // Arrange
        var match = Match.Create(_competitionId, _stageId, _home, _away, _clock);
        var id = match.Id;

        // Act
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        // Assert
        match.Id.Should().Be(id);
        match.CompetitionId.Should().Be(_competitionId);
        match.StageId.Should().Be(_stageId);
        match.HomeEntryId.Should().Be(_home);
        match.AwayEntryId.Should().Be(_away);
    }

    [Fact]
    public void Start_from_Scheduled_transitions_to_Live()
    {
        // Arrange
        var match = CreateScheduled();
        match.ClearDomainEvents();

        // Act
        match.Start(_clock);

        // Assert
        match.Status.Should().Be(MatchStatus.Live);
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchStarted>();
    }

    [Fact]
    public void Start_from_Live_Finished_Cancelled_is_rejected()
    {
        // Arrange
        var live = CreateLive();
        var finished = CreateFinished();
        var cancelled = CreateScheduled();
        cancelled.Cancel(_clock);

        // Act & Assert
        ((Action)(() => live.Start(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => finished.Start(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => cancelled.Start(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Finish_from_Live_records_result_and_raises_MatchFinished()
    {
        // Arrange
        var match = CreateLive();
        match.ClearDomainEvents();
        var result = new MatchResult(
            ResultType.Played,
            new Score(2, 2),
            extraTimePlayed: true,
            new PenaltyShootoutScore(5, 4));

        // Act
        match.Finish(result, _clock);

        // Assert
        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.Result.ExtraTimePlayed.Should().BeTrue();
        match.Result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
        var finished = match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchFinished>().Subject;
        finished.MatchId.Should().Be(match.Id);
        finished.ResultType.Should().Be(ResultType.Played);
        finished.Score.Should().Be(new Score(2, 2));

        // MatchFinished remains lifecycle + Score only (no ExtraTimePlayed / shootout on the event).
        finished.GetType().GetProperty("ExtraTimePlayed").Should().BeNull();
        finished.GetType().GetProperty("PenaltyShootoutScore").Should().BeNull();
    }

    [Fact]
    public void Finish_from_Scheduled_records_result_without_running_score()
    {
        var match = CreateScheduled();
        match.ClearDomainEvents();
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        match.Finish(result, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.RunningScore.Should().BeNull();
        match.HasObservedLive.Should().BeFalse();
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchFinished>();
    }

    [Fact]
    public void Finish_from_Postponed_records_result_without_running_score()
    {
        var match = CreateScheduled();
        match.Postpone(_clock);
        match.ClearDomainEvents();
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        match.Finish(result, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.RunningScore.Should().BeNull();
        match.HasObservedLive.Should().BeFalse();
    }

    [Fact]
    public void Finish_from_Cancelled_is_rejected()
    {
        var cancelled = CreateScheduled();
        cancelled.Cancel(_clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 0));

        var act = () => cancelled.Finish(result, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void CorrectMatchResult_on_Finished_replaces_result_without_status_or_running_score_change()
    {
        var match = CreateFinished();
        var previousRunning = match.RunningScore;
        match.ClearDomainEvents();
        var corrected = new MatchResult(ResultType.Played, new Score(3, 1));

        match.CorrectMatchResult(corrected, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(corrected);
        match.RunningScore.Should().Be(previousRunning);
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchResultCorrected>()
            .Which.Result.Should().Be(corrected);
    }

    [Fact]
    public void CorrectMatchResult_noop_when_identical_raises_no_event()
    {
        var match = CreateFinished();
        var current = match.Result!;
        match.ClearDomainEvents();

        match.CorrectMatchResult(current, _clock);

        match.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CorrectMatchResult_before_Finished_is_rejected()
    {
        var live = CreateLive();
        var result = new MatchResult(ResultType.Played, new Score(1, 0));

        var act = () => live.CorrectMatchResult(result, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Finish_when_already_Finished_is_rejected()
    {
        // Arrange
        var match = CreateFinished();
        var result = new MatchResult(ResultType.Played, new Score(0, 0));

        // Act
        var act = () => match.Finish(result, _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ResultAlreadyRecorded);
    }

    [Fact]
    public void Finish_rejects_null_result()
    {
        // Arrange
        var match = CreateLive();

        // Act
        var act = () => match.Finish(null!, _clock);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Postpone_from_Scheduled_transitions_to_Postponed()
    {
        // Arrange
        var match = CreateScheduled();
        match.ClearDomainEvents();

        // Act
        match.Postpone(_clock);

        // Assert
        match.Status.Should().Be(MatchStatus.Postponed);
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchPostponed>();
    }

    [Fact]
    public void Postpone_from_Live_Finished_Cancelled_is_rejected()
    {
        // Arrange
        var live = CreateLive();
        var finished = CreateFinished();
        var cancelled = CreateScheduled();
        cancelled.Cancel(_clock);

        // Act & Assert
        ((Action)(() => live.Postpone(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => finished.Postpone(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => cancelled.Postpone(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void ResumeSchedule_from_Postponed_returns_to_Scheduled()
    {
        // Arrange
        var match = CreateScheduled();
        match.Postpone(_clock);
        match.ClearDomainEvents();

        // Act
        match.ResumeSchedule(_clock);

        // Assert
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchResumed>();
    }

    [Fact]
    public void ResumeSchedule_from_other_statuses_is_rejected()
    {
        // Arrange
        var scheduled = CreateScheduled();
        var live = CreateLive();
        var finished = CreateFinished();
        var cancelled = CreateScheduled();
        cancelled.Cancel(_clock);

        // Act & Assert
        ((Action)(() => scheduled.ResumeSchedule(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => live.ResumeSchedule(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => finished.ResumeSchedule(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => cancelled.ResumeSchedule(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Cancel_allowed_from_Scheduled_Live_Postponed()
    {
        // Arrange
        var scheduled = CreateScheduled();
        var live = CreateLive();
        var postponed = CreateScheduled();
        postponed.Postpone(_clock);
        scheduled.ClearDomainEvents();
        live.ClearDomainEvents();
        postponed.ClearDomainEvents();

        // Act
        scheduled.Cancel(_clock);
        live.Cancel(_clock);
        postponed.Cancel(_clock);

        // Assert
        scheduled.Status.Should().Be(MatchStatus.Cancelled);
        live.Status.Should().Be(MatchStatus.Cancelled);
        postponed.Status.Should().Be(MatchStatus.Cancelled);
        scheduled.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchCancelled>();
        live.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchCancelled>();
        postponed.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchCancelled>();
    }

    [Fact]
    public void Cancel_from_Finished_or_Cancelled_is_rejected()
    {
        // Arrange
        var finished = CreateFinished();
        var cancelled = CreateScheduled();
        cancelled.Cancel(_clock);

        // Act & Assert
        ((Action)(() => finished.Cancel(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
        ((Action)(() => cancelled.Cancel(_clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Start_from_Postponed_without_ResumeSchedule_is_rejected()
    {
        // Arrange
        var match = CreateScheduled();
        match.Postpone(_clock);

        // Act
        var act = () => match.Start(_clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    private Match CreateScheduled() =>
        Match.Create(_competitionId, _stageId, EntryId.New(), EntryId.New(), _clock);

    private Match CreateLive()
    {
        var match = CreateScheduled();
        match.Start(_clock);
        return match;
    }

    private Match CreateFinished()
    {
        var match = CreateLive();
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return match;
    }
}
