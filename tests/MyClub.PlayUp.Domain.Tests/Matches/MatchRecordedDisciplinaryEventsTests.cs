// -----------------------------------------------------------------------
// <copyright file="MatchRecordedDisciplinaryEventsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

/// <summary>
/// Reference cases 1–13 (FROZEN). Cases 2 and 4 are Application AllowedTypes gates —
/// covered at <see cref="DisciplinaryRules"/> level here.
/// </summary>
public sealed class MatchRecordedDisciplinaryEventsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 30, 20, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();
    private readonly MemberId _dupont = MemberId.New();
    private readonly MemberId _martin = MemberId.New();
    private readonly MemberId _rossi = MemberId.New();

    [Fact]
    public void Case1_yellow_live_has_no_collateral_effects()
    {
        var match = CreateLiveMatchWithSheet();
        match.RecordSubstitution(_dupont, _martin, Side.Home, _clock);
        var runningBefore = match.RunningScore;
        var subsBefore = match.RecordedSubstitutions.Count;
        match.ClearDomainEvents();

        var evt = match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);

        evt.Type.Should().Be(DisciplinaryType.Yellow);
        evt.MemberId.Should().Be(_dupont);
        match.RecordedDisciplinaryEvents.Should().ContainSingle();
        match.RunningScore.Should().Be(runningBefore);
        match.Result.Should().BeNull();
        match.RecordedSubstitutions.Should().HaveCount(subsBefore);
        match.DeclaredParticipations.Should().HaveCount(3);
        match.DomainEvents.OfType<MatchRecordedDisciplinaryEventAdded>().Should().ContainSingle();
    }

    [Fact]
    public void Case2_type_not_in_allowed_types_refused_by_rules()
    {
        var rules = new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]);

        rules.Allows(DisciplinaryType.White).Should().BeFalse();
    }

    [Fact]
    public void Case3_white_is_a_fact_without_domain_tempo_meaning()
    {
        var match = CreateLiveMatchWithSheet();
        var subsBefore = match.RecordedSubstitutions.Count;

        var evt = match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.White, _clock);

        evt.Type.Should().Be(DisciplinaryType.White);
        match.RecordedSubstitutions.Should().HaveCount(subsBefore);
        match.RunningScore.Should().Be(new RunningScore(0, 0));
    }

    [Fact]
    public void Case4_empty_allowed_types_refuses_all()
    {
        DisciplinaryRules.None.Allows(DisciplinaryType.Yellow).Should().BeFalse();
        DisciplinaryRules.None.AllowedTypes.Should().BeEmpty();
    }

    [Fact]
    public void Case5_two_yellows_are_two_facts_without_auto_red()
    {
        var match = CreateLiveMatchWithSheet();

        match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);
        match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);

        match.RecordedDisciplinaryEvents.Should().HaveCount(2);
        match.RecordedDisciplinaryEvents.Should().OnlyContain(e => e.Type == DisciplinaryType.Yellow);
        match.RecordedDisciplinaryEvents.Should().NotContain(e => e.Type == DisciplinaryType.Red);
    }

    [Fact]
    public void Case6_red_has_no_consequence_presence_stays_substitution_derived_only()
    {
        var match = CreateLiveMatchWithSheet();
        var subsBefore = match.RecordedSubstitutions.ToList();
        match.ClearDomainEvents();

        match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Red, _clock);

        match.RecordedSubstitutions.Should().Equal(subsBefore);
        match.RecordedDisciplinaryEvents.Should().ContainSingle(e => e.Type == DisciplinaryType.Red);
        match.DomainEvents.OfType<MatchRecordedSubstitutionAdded>().Should().BeEmpty();
        match.RunningScore.Should().Be(new RunningScore(0, 0));
    }

    [Fact]
    public void Case7_member_not_on_sheet_rejected()
    {
        var match = CreateLiveMatchWithSheet();
        var outsider = MemberId.New();

        var act = () => match.RecordDisciplinaryEvent(outsider, DisciplinaryType.Yellow, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipationNotFound);
    }

    [Fact]
    public void Case8_finished_with_observed_live_correct_only()
    {
        var match = CreateLiveMatchWithSheet();
        var evt = match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedDisciplinaryEvent(evt.Id, _dupont, DisciplinaryType.Red, _clock);

        match.RecordedDisciplinaryEvents.Single().Type.Should().Be(DisciplinaryType.Red);
        match.DomainEvents.OfType<MatchRecordedDisciplinaryEventChanged>().Should().ContainSingle();

        var create = () => match.RecordDisciplinaryEvent(_martin, DisciplinaryType.Yellow, _clock);
        var remove = () => match.RemoveRecordedDisciplinaryEvent(evt.Id, _clock);

        create.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.DisciplinaryEventMutationNotAllowed);
        remove.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.DisciplinaryEventMutationNotAllowed);
    }

    [Fact]
    public void Case9_scheduled_create_ok()
    {
        var match = CreateMatchWithSheet();

        var evt = match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);

        evt.MemberId.Should().Be(_dupont);
        match.Status.Should().Be(MatchStatus.Scheduled);
    }

    [Fact]
    public void Case10_member_id_is_required_by_api_shape_undefined_type_still_rejected()
    {
        var match = CreateLiveMatchWithSheet();
        typeof(Match).GetMethod(
                nameof(Match.RecordDisciplinaryEvent),
                [typeof(MemberId), typeof(DisciplinaryType), typeof(IClock)])!
            .GetParameters()[0]
            .ParameterType.Should().Be<MemberId>();

        var act = () => match.RecordDisciplinaryEvent(_dupont, (DisciplinaryType)99, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidDisciplinaryType);
    }

    [Fact]
    public void Case11_any_declared_participation_is_domain_neutral_including_staff_shaped_sheet()
    {
        var match = CreateLiveMatchWithSheet();

        var evt = match.RecordDisciplinaryEvent(_martin, DisciplinaryType.Yellow, _clock);

        evt.MemberId.Should().Be(_martin);
        match.HasDeclaredParticipation(_martin).Should().BeTrue();
    }

    [Fact]
    public void Case12_remove_participation_referenced_by_disciplinary_event_refused()
    {
        var match = CreateMatchWithSheet();
        match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);

        var act = () => match.RemoveDeclaredParticipation(_dupont, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should()
            .Be(MatchErrorCodes.ParticipationReferencedByDisciplinaryEvent);
    }

    [Fact]
    public void Case13_recording_red_does_not_create_stage_penalty_or_mutate_scores()
    {
        var match = CreateLiveMatchWithSheet();
        var runningBefore = match.RunningScore;

        match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Red, _clock);

        match.RunningScore.Should().Be(runningBefore);
        match.Result.Should().BeNull();
        match.RecordedGoals.Should().BeEmpty();
    }

    [Fact]
    public void Correct_noop_does_not_raise_event()
    {
        var match = CreateLiveMatchWithSheet();
        var evt = match.RecordDisciplinaryEvent(_dupont, DisciplinaryType.Yellow, _clock);
        match.ClearDomainEvents();

        match.CorrectRecordedDisciplinaryEvent(evt.Id, _dupont, DisciplinaryType.Yellow, _clock);

        match.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Regulation_defaults_to_no_disciplinary_types() => SampleRegulations.Standard().DisciplinaryRules.Should().Be(DisciplinaryRules.None);

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
