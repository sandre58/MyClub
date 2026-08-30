// -----------------------------------------------------------------------
// <copyright file="MatchRecordedSubstitutionsTests.cs" company="Stéphane ANDRE">
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
/// Reference cases A–X — Domain #6 Remplacements (Décision Acceptée + PD P-S0–P-S8).
/// </summary>
public sealed class MatchRecordedSubstitutionsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 30, 21, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly MemberId _dupont = MemberId.New();
    private readonly MemberId _martin = MemberId.New();
    private readonly MemberId _bernard = MemberId.New();
    private readonly MemberId _rossi = MemberId.New();

    [Fact]
    public void A1_live_substitution_does_not_mutate_composition_status()
    {
        var match = CreateLiveWithHomeSheet();
        var dupontStatus = match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus;
        var martinStatus = match.DeclaredParticipations.Single(p => p.Id.Equals(_martin)).CompositionStatus;
        match.ClearDomainEvents();

        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus.Should().Be(dupontStatus);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_martin)).CompositionStatus.Should().Be(martinStatus);
        var changeStatus = () => match.ChangeDeclaredParticipationCompositionStatus(
            _dupont, CompositionStatus.Bench, _clock);
        changeStatus.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
        match.DomainEvents.OfType<MatchRecordedSubstitutionAdded>().Should().ContainSingle();
    }

    [Fact]
    public void A2_multiple_substitutions_leave_sheet_declarative()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.RecordSubstitution(_martin, _bernard, Side.Home, _clock);

        match.RecordedSubstitutions.Should().HaveCount(2);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus
            .Should().Be(CompositionStatus.Starter);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_martin)).CompositionStatus
            .Should().Be(CompositionStatus.Bench);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_bernard)).CompositionStatus
            .Should().Be(CompositionStatus.Bench);
    }

    [Fact]
    public void B1_order_is_preserved_without_minute()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        var first = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        var second = match.RecordSubstitution(_martin, _bernard, Side.Home, _clock);

        match.RecordedSubstitutions.Select(s => s.Id).Should().Equal(first.Id, second.Id);
        typeof(RecordedSubstitution).GetProperty("Minute").Should().BeNull();
    }

    [Fact]
    public void B2_correct_last_and_remove_keep_sheet_unchanged()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        _ = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        var second = match.RecordSubstitution(_martin, _bernard, Side.Home, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedSubstitution(second.Id, _martin, _bernard, Side.Home, _clock);
        match.DomainEvents.Should().BeEmpty();

        match.RemoveRecordedSubstitution(second.Id, _clock);

        match.RecordedSubstitutions.Should().ContainSingle();
        match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus
            .Should().Be(CompositionStatus.Starter);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_martin)).CompositionStatus
            .Should().Be(CompositionStatus.Bench);
    }

    [Fact]
    public void B2_correct_middle_fact_revalidates_full_sequence()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        var first = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        _ = match.RecordSubstitution(_martin, _bernard, Side.Home, _clock);

        var act = () => match.CorrectRecordedSubstitution(first.Id, _dupont, _bernard, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionPresenceInvalid);
    }

    [Fact]
    public void B3_out_only_or_in_only_is_not_a_substitution_api()
    {
        // Atomic couple is enforced by the API surface (no single-member operation).
        typeof(Match).GetMethod(nameof(Match.RecordSubstitution), [typeof(MemberId), typeof(Side), typeof(IClock)])
            .Should().BeNull();
    }

    [Fact]
    public void C1_cross_side_substitution_is_rejected()
    {
        var match = CreateLiveWithHomeAndAwaySheet();

        var act = () => match.RecordSubstitution(_dupont, _rossi, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionSideMismatch);
    }

    [Fact]
    public void C2_member_not_on_sheet_is_rejected()
    {
        var match = CreateLiveWithHomeSheet();
        var outsider = MemberId.New();

        var act = () => match.RecordSubstitution(_dupont, outsider, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipationNotFound);
    }

    [Fact]
    public void C3_bench_cannot_leave_when_starter_already_on_field()
    {
        var match = CreateLiveWithHomeSheet();

        var act = () => match.RecordSubstitution(_martin, _dupont, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionPresenceInvalid);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_martin)).CompositionStatus
            .Should().Be(CompositionStatus.Bench);
    }

    [Fact]
    public void C4_reentry_is_rejected()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        var act = () => match.RecordSubstitution(_bernard, _dupont, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionPresenceInvalid);
    }

    [Fact]
    public void C4a_double_exit_is_rejected()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        var act = () => match.RecordSubstitution(_dupont, _bernard, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionPresenceInvalid);
    }

    [Fact]
    public void C4b_double_entry_is_rejected()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        var act = () => match.RecordSubstitution(_bernard, _martin, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionPresenceInvalid);
    }

    [Fact]
    public void C5_out_equals_in_is_rejected()
    {
        var match = CreateLiveWithHomeSheet();

        var act = () => match.RecordSubstitution(_dupont, _dupont, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionSameMember);
    }

    [Fact]
    public void D1_scheduled_rejects_substitution()
    {
        var match = CreateScheduledWithHomeSheet();

        var act = () => match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionMutationNotAllowed);
    }

    [Fact]
    public void D1b_postponed_rejects_substitution()
    {
        var match = CreateScheduledWithHomeSheet();
        match.Postpone(_clock);

        var act = () => match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionMutationNotAllowed);
    }

    [Fact]
    public void D2_finished_without_observed_live_allows_reconstruction()
    {
        var match = CreateScheduledWithHomeSheet();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        var substitution = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        match.HasObservedLive.Should().BeFalse();
        match.RecordedSubstitutions.Should().ContainSingle().Which.Id.Should().Be(substitution.Id);
        match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus
            .Should().Be(CompositionStatus.Starter);
    }

    [Fact]
    public void D3_finished_with_observed_live_allows_correct_only()
    {
        var match = CreateLiveWithHomeSheet();
        var substitution = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        match.ClearDomainEvents();

        var create = () => match.RecordSubstitution(_martin, _dupont, Side.Home, _clock);
        var remove = () => match.RemoveRecordedSubstitution(substitution.Id, _clock);

        create.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionMutationNotAllowed);
        remove.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionMutationNotAllowed);

        // Correct needs a valid alternative: only two members — correcting to same is noop.
        // Add Bernard on sheet before finish path:
        var match2 = CreateLiveWithHomeSheet(includeBernard: true);
        var sub = match2.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match2.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        match2.ClearDomainEvents();
        match2.CorrectRecordedSubstitution(sub.Id, _dupont, _bernard, Side.Home, _clock);

        sub.InMemberId.Should().Be(_bernard);
        match2.DomainEvents.OfType<MatchRecordedSubstitutionChanged>().Should().ContainSingle();
    }

    [Fact]
    public void D4_correct_after_finish_without_live_leaves_result_unchanged()
    {
        var match = CreateScheduledWithHomeSheet(includeBernard: true);
        var result = new MatchResult(ResultType.Played, new Score(2, 1));
        match.Finish(result, _clock);
        var sub = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedSubstitution(sub.Id, _dupont, _bernard, Side.Home, _clock);

        match.Result.Should().Be(result);
        match.RunningScore.Should().BeNull();
        match.DeclaredParticipations.Single(p => p.Id.Equals(_dupont)).CompositionStatus
            .Should().Be(CompositionStatus.Starter);
    }

    [Fact]
    public void D5_live_supports_create_correct_remove()
    {
        var match = CreateLiveWithHomeSheet(includeBernard: true);
        var sub = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.ClearDomainEvents();
        match.CorrectRecordedSubstitution(sub.Id, _dupont, _bernard, Side.Home, _clock);
        match.ClearDomainEvents();
        match.RemoveRecordedSubstitution(sub.Id, _clock);

        match.RecordedSubstitutions.Should().BeEmpty();
        match.DomainEvents.OfType<MatchRecordedSubstitutionRemoved>().Should().ContainSingle();
    }

    [Fact]
    public void E1_quota_is_not_enforced()
    {
        var extra1 = MemberId.New();
        var extra2 = MemberId.New();
        var match = CreateScheduled();
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(_bernard, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(extra1, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(extra2, Side.Home, CompositionStatus.Bench, _clock);
        match.Start(_clock);

        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.RecordSubstitution(_martin, _bernard, Side.Home, _clock);
        match.RecordSubstitution(_bernard, extra1, Side.Home, _clock);
        match.RecordSubstitution(extra1, extra2, Side.Home, _clock);

        match.RecordedSubstitutions.Should().HaveCount(4);
    }

    [Fact]
    public void E2_no_automatic_substitution_surface_for_cards()
    {
        typeof(Match).GetMethods().Select(m => m.Name)
            .Should().NotContain(name => name.Contains("Card", StringComparison.Ordinal)
                                         || name.Contains("Disciplinary", StringComparison.Ordinal));
    }

    [Fact]
    public void X1_no_public_isonfield_and_no_onfield_collection()
    {
        typeof(Match).GetMethod("IsOnField").Should().BeNull();
        typeof(Match).GetProperty("OnFieldMembers").Should().BeNull();
    }

    [Fact]
    public void X2_substitution_does_not_sync_scores_or_goals()
    {
        var match = CreateLiveWithHomeSheet();
        match.RecordGoal(_dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(0, 0), _clock);
        var running = match.RunningScore;
        var goals = match.RecordedGoals.ToList();

        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(5, 0)), _clock);

        match.RunningScore.Should().Be(running);
        match.RecordedGoals.Should().Equal(goals);
        match.Result!.Score.Should().Be(new Score(5, 0));
    }

    [Fact]
    public void Correct_noop_raises_no_event()
    {
        var match = CreateLiveWithHomeSheet();
        var sub = match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedSubstitution(sub.Id, _dupont, _martin, Side.Home, _clock);

        match.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Remove_participation_referenced_by_substitution_is_rejected()
    {
        var match = CreateScheduledWithHomeSheet();
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        var act = () => match.RemoveDeclaredParticipation(_dupont, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.ParticipationReferencedBySubstitution);
    }

    [Fact]
    public void Cancelled_rejects_substitution()
    {
        var match = CreateLiveWithHomeSheet();
        match.Cancel(_clock);

        var act = () => match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SubstitutionMutationNotAllowed);
    }

    private Match CreateScheduled() =>
        Match.Create(_competitionId, _stageId, EntryId.New(), EntryId.New(), _clock);

    private Match CreateScheduledWithHomeSheet(bool includeBernard = false)
    {
        var match = CreateScheduled();
        match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, _clock);
        if (includeBernard)
        {
            match.AddDeclaredParticipation(_bernard, Side.Home, CompositionStatus.Bench, _clock);
        }

        return match;
    }

    private Match CreateLiveWithHomeSheet(bool includeBernard = false)
    {
        var match = CreateScheduledWithHomeSheet(includeBernard);
        match.Start(_clock);
        match.ClearDomainEvents();
        return match;
    }

    private Match CreateLiveWithHomeAndAwaySheet()
    {
        var match = CreateScheduledWithHomeSheet();
        match.AddDeclaredParticipation(_rossi, Side.Away, CompositionStatus.Starter, _clock);
        match.Start(_clock);
        match.ClearDomainEvents();
        return match;
    }
}
