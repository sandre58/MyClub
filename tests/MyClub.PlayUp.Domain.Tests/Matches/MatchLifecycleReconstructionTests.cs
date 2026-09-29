// -----------------------------------------------------------------------
// <copyright file="MatchLifecycleReconstructionTests.cs" company="Stéphane ANDRE">
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

/// <summary>
/// Lifecycle / state reconstruction reference cases (Live open, finish, sheet freeze, forfeit, cancellation).
/// </summary>
public sealed class MatchLifecycleReconstructionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 30, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly MemberId _dupont = MemberId.New();
    private readonly MemberId _martin = MemberId.New();
    private readonly MemberId _rossi = MemberId.New();

    [Fact]
    public void L1_scheduled_live_set_running_score_finish()
    {
        var match = CreateScheduled();
        match.Start(_clock);
        match.SetRunningScore(new RunningScore(2, 1), _clock);
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        match.Finish(result, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.RunningScore.Should().Be(new RunningScore(2, 1));
        match.HasObservedLive.Should().BeTrue();
    }

    [Fact]
    public void L2_after_observed_live_finish_sheet_is_frozen()
    {
        var match = FinishAfterObservedLive();

        var act = () => match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
    }

    [Fact]
    public void L3_after_observed_live_finish_record_goal_is_rejected()
    {
        var match = FinishAfterObservedLiveWithSheetAndGoal();

        var act = () => match.RecordGoal(_martin, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RecordedGoalMutationNotAllowed);
    }

    [Fact]
    public void L4_after_observed_live_finish_correct_existing_goal_keeps_result_and_running_score()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(1, 0), _clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 0));
        match.Finish(result, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedGoal(goal.Id, _martin, Side.Home, _clock);

        goal.ScorerMemberId.Should().Be(_martin);
        match.Result.Should().Be(result);
        match.RunningScore.Should().Be(new RunningScore(1, 0));
    }

    [Fact]
    public void L5_after_observed_live_finish_correct_match_result_keeps_running_score_and_goals()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(1, 0), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var goalsBefore = match.RecordedGoals.ToList();
        match.ClearDomainEvents();
        var corrected = new MatchResult(ResultType.Played, new Score(2, 0));

        match.CorrectMatchResult(corrected, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(corrected);
        match.RunningScore.Should().Be(new RunningScore(1, 0));
        match.RecordedGoals.Should().Equal(goalsBefore);
        match.DomainEvents.OfType<MatchResultCorrected>().Should().ContainSingle();
    }

    [Fact]
    public void L6_correct_result_after_live_finish_leaves_running_score_and_goals_unchanged()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(2, 1), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        var goalsBefore = match.RecordedGoals.ToList();

        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);

        match.RunningScore.Should().Be(new RunningScore(2, 1));
        match.RecordedGoals.Should().Equal(goalsBefore);
        match.Result!.Score.Should().Be(new Score(3, 1));
    }

    [Fact]
    public void N1_scheduled_finish_without_start_leaves_running_score_null()
    {
        var match = CreateScheduled();
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        match.Finish(result, _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.Result.Should().Be(result);
        match.RunningScore.Should().BeNull();
        match.HasObservedLive.Should().BeFalse();
    }

    [Fact]
    public void N2_postponed_finish_without_resume_or_start()
    {
        var match = CreateScheduled();
        match.Postpone(_clock);

        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.RunningScore.Should().BeNull();
        match.HasObservedLive.Should().BeFalse();
    }

    [Fact]
    public void N3_finished_without_live_allows_sheet_and_record_goal()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);

        match.HasDeclaredParticipation(_dupont).Should().BeTrue();
        match.RecordedGoals.Should().ContainSingle().Which.Id.Should().Be(goal.Id);
    }

    [Fact]
    public void N4_record_goal_without_sheet_is_rejected()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        var act = () => match.RecordGoal(_dupont, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipationNotFound);
    }

    [Fact]
    public void N5_remove_sheet_member_referenced_by_recorded_goal_is_rejected()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.RecordGoal(_dupont, Side.Home, _clock);

        var act = () => match.RemoveDeclaredParticipation(_dupont, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.ParticipationReferencedByRecordedGoal);
        match.HasDeclaredParticipation(_dupont).Should().BeTrue();
    }

    [Fact]
    public void N6_remove_recorded_goal_then_remove_sheet_succeeds()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);

        match.RemoveRecordedGoal(goal.Id, _clock);
        match.RemoveDeclaredParticipation(_dupont, _clock);

        match.RecordedGoals.Should().BeEmpty();
        match.HasDeclaredParticipation(_dupont).Should().BeFalse();
    }

    [Fact]
    public void N7_correct_match_result_without_live_keeps_running_score_null()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);

        match.Result!.Score.Should().Be(new Score(3, 1));
        match.RunningScore.Should().BeNull();
        match.RecordedGoals.Should().BeEmpty();
    }

    [Fact]
    public void N8_sheet_and_goal_before_finish_without_start()
    {
        var match = CreateScheduledWithSheet();
        match.RecordGoal(_dupont, Side.Home, _clock);

        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        match.RunningScore.Should().BeNull();
        match.HasObservedLive.Should().BeFalse();
        match.RecordedGoals.Should().ContainSingle();
    }

    [Fact]
    public void N9_finished_without_live_allows_composition_status_and_jersey_mutations()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(0, 0)), _clock);
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 9);

        match.ChangeDeclaredParticipationCompositionStatus(_dupont, CompositionStatus.Bench, _clock);
        match.SetDeclaredParticipationJerseyNumber(_dupont, 10, _clock);

        var participation = match.DeclaredParticipations.Single();
        participation.CompositionStatus.Should().Be(CompositionStatus.Bench);
        participation.JerseyNumber.Should().Be(10);
    }

    [Fact]
    public void N10_correct_result_then_sheet_and_goal_remain_independent_dimensions()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);

        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.RecordGoal(_dupont, Side.Home, _clock);

        match.Result!.Score.Should().Be(new Score(3, 1));
        match.RunningScore.Should().BeNull();
        match.RecordedGoals.Should().ContainSingle();
    }

    [Fact]
    public void X1_cancelled_finish_is_rejected()
    {
        var match = CreateScheduled();
        match.Cancel(_clock);

        var act = () => match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void X2_finished_start_is_rejected()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var act = () => match.Start(_clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidTransition);
    }

    [Fact]
    public void X3_result_running_score_and_recorded_goals_need_not_align()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(0, 0), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(5, 0)), _clock);

        match.Result!.Score.Should().Be(new Score(5, 0));
        match.RunningScore.Should().Be(new RunningScore(0, 0));
        match.RecordedGoals.Should().ContainSingle();
    }

    [Fact]
    public void X4_start_is_not_required_to_reach_finished_from_scheduled()
    {
        var match = CreateScheduled();

        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        match.HasObservedLive.Should().BeFalse();
    }

    [Fact]
    public void X5_correct_result_does_not_sync_running_score_or_goals()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(1, 0), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        var goalsBefore = match.RecordedGoals.ToList();

        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);

        match.RunningScore.Should().Be(new RunningScore(1, 0));
        match.RecordedGoals.Should().Equal(goalsBefore);
        match.Result!.Score.Should().Be(new Score(3, 1));
    }

    [Fact]
    public void N5_assister_reference_also_blocks_remove()
    {
        var match = CreateScheduled();
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, _clock);
        match.RecordGoal(_dupont, Side.Home, _clock, assisterMemberId: _martin);

        var act = () => match.RemoveDeclaredParticipation(_martin, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.ParticipationReferencedByRecordedGoal);
    }

    private Match CreateScheduled() =>
        Match.Create(_competitionId, _stageId, EntryId.New(), EntryId.New(), _clock);

    private Match CreateScheduledWithSheet()
    {
        var match = CreateScheduled();
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(_rossi, Side.Away, CompositionStatus.Starter, _clock);
        return match;
    }

    private Match FinishAfterObservedLive()
    {
        var match = CreateScheduled();
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return match;
    }

    private Match FinishAfterObservedLiveWithSheetAndGoal()
    {
        var match = CreateScheduledWithSheet();
        match.Start(_clock);
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return match;
    }
}
