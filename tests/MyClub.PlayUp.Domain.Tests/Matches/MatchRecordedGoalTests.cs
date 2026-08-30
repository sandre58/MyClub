// -----------------------------------------------------------------------
// <copyright file="MatchRecordedGoalTests.cs" company="Stéphane ANDRE">
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

public sealed class MatchRecordedGoalTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();
    private readonly MemberId _dupont = MemberId.New();
    private readonly MemberId _martin = MemberId.New();
    private readonly MemberId _rossi = MemberId.New();

    [Fact]
    public void Case1_record_home_goal_without_assister_does_not_change_running_score()
    {
        var match = CreateLiveMatchWithSheet();
        var runningBefore = match.RunningScore;

        var goal = match.RecordGoal(_dupont, Side.Home, _clock);

        goal.ScorerMemberId.Should().Be(_dupont);
        goal.CreditedSide.Should().Be(Side.Home);
        goal.AssisterMemberId.Should().BeNull();
        match.RecordedGoals.Should().ContainSingle();
        match.RunningScore.Should().Be(runningBefore);
        match.DomainEvents.OfType<MatchRecordedGoalAdded>().Should().ContainSingle();
    }

    [Fact]
    public void Case2_set_running_score_after_recorded_goal_updates_counter_separately()
    {
        var match = CreateLiveMatchWithSheet();
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.ClearDomainEvents();

        match.SetRunningScore(new RunningScore(1, 0), _clock);

        match.RunningScore.Should().Be(new RunningScore(1, 0));
        match.RecordedGoals.Should().ContainSingle();
        match.DomainEvents.OfType<MatchRunningScoreChanged>().Should().ContainSingle();
        match.DomainEvents.OfType<MatchRecordedGoalAdded>().Should().BeEmpty();
    }

    [Fact]
    public void Case3_own_goal_via_credited_side_away_for_home_scorer()
    {
        var match = CreateLiveMatchWithSheet();
        var runningBefore = match.RunningScore;

        var goal = match.RecordGoal(_dupont, Side.Away, _clock);

        match.IsOwnGoal(goal).Should().BeTrue();
        match.RunningScore.Should().Be(runningBefore);
    }

    [Fact]
    public void Case4_assister_forbidden_on_own_goal()
    {
        var match = CreateLiveMatchWithSheet();

        var act = () => match.RecordGoal(_dupont, Side.Away, _clock, assisterMemberId: _martin);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.AssisterNotAllowedOnOwnGoal);
        match.RecordedGoals.Should().BeEmpty();
    }

    [Fact]
    public void Case5_optional_assister_on_normal_goal()
    {
        var match = CreateLiveMatchWithSheet();

        var goal = match.RecordGoal(_dupont, Side.Home, _clock, assisterMemberId: _martin);

        goal.AssisterMemberId.Should().Be(_martin);
        match.IsOwnGoal(goal).Should().BeFalse();
    }

    [Fact]
    public void Case6_scorer_not_on_sheet_is_rejected()
    {
        var match = CreateLiveMatchWithSheet();
        var outsider = MemberId.New();

        var act = () => match.RecordGoal(outsider, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipationNotFound);
    }

    [Fact]
    public void Case7_undefined_credited_side_is_rejected()
    {
        var match = CreateLiveMatchWithSheet();

        var act = () => match.RecordGoal(_dupont, (Side)99, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidSide);
    }

    [Fact]
    public void Case8_record_goal_allowed_while_scheduled()
    {
        var match = CreateMatchWithSheet();

        var goal = match.RecordGoal(_dupont, Side.Home, _clock);

        goal.ScorerMemberId.Should().Be(_dupont);
        match.RecordedGoals.Should().ContainSingle();
    }

    [Fact]
    public void Case9_live_allows_create_and_correct()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedGoal(goal.Id, _dupont, Side.Home, _clock, assisterMemberId: _martin);

        match.RecordedGoals.Single().AssisterMemberId.Should().Be(_martin);
        match.DomainEvents.OfType<MatchRecordedGoalChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Case10_after_finish_with_observed_live_create_and_remove_are_rejected()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var record = () => match.RecordGoal(_rossi, Side.Away, _clock);
        var remove = () => match.RemoveRecordedGoal(goal.Id, _clock);

        record.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RecordedGoalMutationNotAllowed);
        remove.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RecordedGoalMutationNotAllowed);
    }

    [Fact]
    public void Case11_after_finish_with_observed_live_correct_does_not_align_match_result_score()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        var official = new MatchResult(ResultType.Played, new Score(3, 0));
        match.Finish(official, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedGoal(goal.Id, _dupont, Side.Home, _clock, assisterMemberId: _martin);

        match.RecordedGoals.Single().AssisterMemberId.Should().Be(_martin);
        match.Result.Should().Be(official);
        match.DomainEvents.OfType<MatchRecordedGoalChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Case12_cancelled_rejects_nominative_mutations()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.Cancel(_clock);

        var record = () => match.RecordGoal(_rossi, Side.Away, _clock);
        var correct = () => match.CorrectRecordedGoal(goal.Id, _dupont, Side.Home, _clock);

        record.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RecordedGoalMutationNotAllowed);
        correct.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.RecordedGoalMutationNotAllowed);
    }

    [Fact]
    public void Case13_finish_allows_divergence_between_recorded_goals_and_official_score()
    {
        var match = CreateLiveMatchWithSheet();
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.RecordGoal(_rossi, Side.Away, _clock);
        var official = new MatchResult(ResultType.Played, new Score(5, 0));

        match.Finish(official, _clock);

        match.RecordedGoals.Should().HaveCount(2);
        match.Result!.Score.Should().Be(new Score(5, 0));
    }

    [Fact]
    public void Case15_is_own_goal_is_derived_not_stored()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Away, _clock);

        match.IsOwnGoal(goal).Should().BeTrue();
        typeof(RecordedGoal).GetProperty("IsOwnGoal").Should().BeNull();
    }

    [Fact]
    public void Case16_normal_away_goal_with_credited_side_away()
    {
        var match = CreateLiveMatchWithSheet();
        var runningBefore = match.RunningScore;

        var goal = match.RecordGoal(_rossi, Side.Away, _clock);

        goal.CreditedSide.Should().Be(Side.Away);
        match.IsOwnGoal(goal).Should().BeFalse();
        match.RunningScore.Should().Be(runningBefore);
    }

    [Fact]
    public void Case17_assister_not_on_sheet_is_rejected()
    {
        var match = CreateLiveMatchWithSheet();
        var outsider = MemberId.New();

        var act = () => match.RecordGoal(_dupont, Side.Home, _clock, assisterMemberId: outsider);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipationNotFound);
        match.RecordedGoals.Should().BeEmpty();
    }

    [Fact]
    public void Assister_same_as_scorer_is_rejected()
    {
        var match = CreateLiveMatchWithSheet();

        var act = () => match.RecordGoal(_dupont, Side.Home, _clock, assisterMemberId: _dupont);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.AssisterSameAsScorer);
    }

    [Fact]
    public void Remove_recorded_goal_while_live_raises_removed_event()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.ClearDomainEvents();

        match.RemoveRecordedGoal(goal.Id, _clock);

        match.RecordedGoals.Should().BeEmpty();
        match.DomainEvents.OfType<MatchRecordedGoalRemoved>().Should().ContainSingle()
            .Which.GoalId.Should().Be(goal.Id);
    }

    [Fact]
    public void Correct_noop_does_not_raise_changed_event()
    {
        var match = CreateLiveMatchWithSheet();
        var goal = match.RecordGoal(_dupont, Side.Home, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedGoal(goal.Id, _dupont, Side.Home, _clock);

        match.DomainEvents.OfType<MatchRecordedGoalChanged>().Should().BeEmpty();
    }

    private Match CreateMatchWithSheet()
    {
        var match = Match.Create(_competitionId, _stageId, _home, _away, _clock);
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(_rossi, Side.Away, CompositionStatus.Starter, _clock);
        match.ClearDomainEvents();
        return match;
    }

    private Match CreateLiveMatchWithSheet()
    {
        var match = CreateMatchWithSheet();
        match.Start(_clock);
        match.ClearDomainEvents();
        return match;
    }
}
