// -----------------------------------------------------------------------
// <copyright file="RecordedFactsUseCaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class RecordedFactsUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 8, 40, 0, TimeSpan.Zero));

    [Fact]
    public void RecordGoal_adds_nominative_goal_without_mutating_running_score()
    {
        var (match, _, dupont, _) = CreateLiveMatchWithHomeSheet();
        var runningBefore = match.RunningScore;

        var goal = RecordGoal.Execute(match, dupont, Side.Home, _clock);

        goal.ScorerMemberId.Should().Be(dupont);
        match.RecordedGoals.Should().ContainSingle();
        match.RunningScore.Should().Be(runningBefore);
    }

    [Fact]
    public void RecordGoal_rejects_unknown_side()
    {
        var (match, _, dupont, _) = CreateLiveMatchWithHomeSheet();

        var act = () => RecordGoal.Execute(match, dupont, (Side)99, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidSide);
    }

    [Fact]
    public void CorrectRecordedGoal_and_remove_work()
    {
        var (match, _, dupont, martin) = CreateLiveMatchWithHomeSheet();
        var goal = RecordGoal.Execute(match, dupont, Side.Home, _clock);

        CorrectRecordedGoal.Execute(match, goal.Id, martin, Side.Home, _clock);
        match.RecordedGoals.Single().ScorerMemberId.Should().Be(martin);

        RemoveRecordedGoal.Execute(match, goal.Id, _clock);
        match.RecordedGoals.Should().BeEmpty();
    }

    [Fact]
    public void RecordSubstitution_adds_ordered_fact_on_live_match()
    {
        var (match, _, dupont, martin) = CreateLiveMatchWithHomeSheet();

        var substitution = RecordSubstitution.Execute(match, dupont, martin, Side.Home, _clock);

        substitution.OutMemberId.Should().Be(dupont);
        substitution.InMemberId.Should().Be(martin);
        match.RecordedSubstitutions.Should().ContainSingle();
    }

    [Fact]
    public void RecordDisciplinaryEvent_adds_fact_when_type_allowed()
    {
        var (match, competition, dupont, _) = CreateLiveMatchWithHomeSheet(
            disciplinaryRules: new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));

        var evt = RecordDisciplinaryEvent.Execute(match, competition, dupont, DisciplinaryType.Yellow, _clock);

        evt.Type.Should().Be(DisciplinaryType.Yellow);
        match.RecordedDisciplinaryEvents.Should().ContainSingle();
    }

    [Fact]
    public void RecordDisciplinaryEvent_rejects_type_not_in_allowed_types()
    {
        var (match, competition, dupont, _) = CreateLiveMatchWithHomeSheet(
            disciplinaryRules: new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));

        var act = () => RecordDisciplinaryEvent.Execute(match, competition, dupont, DisciplinaryType.White, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DisciplinaryTypeNotAllowed);
    }

    [Fact]
    public void RecordDisciplinaryEvent_rejects_when_no_types_allowed()
    {
        var (match, competition, dupont, _) = CreateLiveMatchWithHomeSheet();

        var act = () => RecordDisciplinaryEvent.Execute(match, competition, dupont, DisciplinaryType.Yellow, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DisciplinaryTypeNotAllowed);
    }

    [Fact]
    public void CorrectRecordedDisciplinaryEvent_rejects_disallowed_new_type()
    {
        var (match, competition, dupont, _) = CreateLiveMatchWithHomeSheet(
            disciplinaryRules: new DisciplinaryRules([DisciplinaryType.Yellow]));
        var evt = RecordDisciplinaryEvent.Execute(match, competition, dupont, DisciplinaryType.Yellow, _clock);

        var act = () => CorrectRecordedDisciplinaryEvent.Execute(
            match, competition, evt.Id, dupont, DisciplinaryType.Red, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DisciplinaryTypeNotAllowed);
    }

    [Fact]
    public void RemoveRecordedDisciplinaryEvent_removes_fact()
    {
        var (match, competition, dupont, _) = CreateLiveMatchWithHomeSheet(
            disciplinaryRules: new DisciplinaryRules([DisciplinaryType.Yellow]));
        var evt = RecordDisciplinaryEvent.Execute(match, competition, dupont, DisciplinaryType.Yellow, _clock);

        RemoveRecordedDisciplinaryEvent.Execute(match, evt.Id, _clock);

        match.RecordedDisciplinaryEvents.Should().BeEmpty();
    }

    private (Match Match, Competition Competition, MemberId Dupont, MemberId Martin) CreateLiveMatchWithHomeSheet(
        DisciplinaryRules? disciplinaryRules = null)
    {
        var standard = SampleRegulations.Standard();
        var regulation = new Regulation(
            standard.EntryRules,
            standard.MatchRules,
            standard.StandingRules,
            disciplinaryRules ?? DisciplinaryRules.None);
        var competition = CreateCompetition.Execute("Facts Cup", regulation, _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var dupont = AddDeclaredMember.Execute(competition, home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var martin = AddDeclaredMember.Execute(competition, home.Id, "Martin", DeclaredMemberRole.Player, _clock);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(dupont.Id, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin.Id, Side.Home, CompositionStatus.Bench, _clock);
        match.Start(_clock);

        return (match, competition, dupont.Id, martin.Id);
    }
}
