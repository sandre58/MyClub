// -----------------------------------------------------------------------
// <copyright file="MatchReadAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class MatchReadAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 20, 0, TimeSpan.Zero));

    [Fact]
    public void AssembleSummaries_orders_by_fixture_and_maps_scores_and_names()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var otherHome = competition.AddEntry(TeamId.New(), "Other Home", _clock);
        var otherAway = competition.AddEntry(TeamId.New(), "Other Away", _clock);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture1 = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var fixture2 = stage.AddFixture(stage.Rounds[0].Id, _clock);

        var match2 = Match.Create(competition.Id, stage.Id, otherHome.Id, otherAway.Id, _clock);
        var match1 = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match1.Start(_clock);
        match1.Finish(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);

        stage.AttachMatch(fixture1.Id, match1.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture2.Id, match2.Id, legIndex: 1, _clock);

        var summaries = MatchReadAssembler.AssembleSummaries(stage, competition, [match2, match1]);

        summaries.Should().HaveCount(2);
        summaries[0].MatchId.Should().Be(match1.Id.Value);
        summaries[0].FixtureId.Should().Be(fixture1.Id.Value);
        summaries[0].RoundId.Should().Be(stage.Rounds[0].Id.Value);
        summaries[0].Home.DisplayName.Should().Be("Home");
        summaries[0].Away.DisplayName.Should().Be("Away");
        summaries[0].Score.Should().Be(new MatchScoreDto(3, 1));
        summaries[0].Status.Should().Be(MatchStatus.Finished);
        summaries[0].ResultType.Should().Be(ResultType.Played);
        summaries[0].RoundName.Should().Be("R1");
        summaries[0].MatchdayNumber.Should().BeNull();
        summaries[1].MatchId.Should().Be(match2.Id.Value);
        summaries[1].Score.Should().BeNull();
        summaries[1].Status.Should().Be(MatchStatus.Scheduled);
        summaries[1].ResultType.Should().BeNull();
        summaries[1].RoundName.Should().Be("R1");
        summaries[0].ScheduledAt.Should().BeNull();
        summaries[0].ResourceId.Should().BeNull();
        summaries[1].ScheduledAt.Should().BeNull();
        summaries[1].ResourceId.Should().BeNull();
    }

    [Fact]
    public void AssembleSummaries_maps_matchday_number_for_championship_fixtures()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var matchday = stage.AddMatchday(2, _clock);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Forfeit, new Score(3, 0)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var summaries = MatchReadAssembler.AssembleSummaries(stage, competition, [match]);

        summaries.Should().ContainSingle();
        summaries[0].MatchdayNumber.Should().Be(2);
        summaries[0].RoundName.Should().BeNull();
        summaries[0].RoundId.Should().BeNull();
        summaries[0].ResultType.Should().Be(ResultType.Forfeit);
        MatchReadAssembler.ConsultationContextLabel(summaries[0]).Should().Be("Journée 2");
    }

    [Fact]
    public void AssembleSummaries_and_detail_map_optional_calendar_placement()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var kickoff = new DateTimeOffset(2026, 9, 1, 15, 0, 0, TimeSpan.Zero);
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements(
            [new MatchPlacement(match.Id, kickoff, resourceId)],
            [match.Id]);

        var summaries = MatchReadAssembler.AssembleSummaries(stage, competition, [match]);
        var detail = MatchReadAssembler.AssembleDetail(match, competition, stage);

        summaries.Should().ContainSingle();
        summaries[0].ScheduledAt.Should().Be(kickoff);
        summaries[0].ResourceId.Should().Be(resourceId.Value);
        detail.ScheduledAt.Should().Be(kickoff);
        detail.ResourceId.Should().Be(resourceId.Value);
    }

    [Fact]
    public void AssembleSummaries_when_no_matches_returns_empty()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);

        MatchReadAssembler.AssembleSummaries(stage, competition, Array.Empty<Match>()).Should().BeEmpty();
    }

    [Fact]
    public void AssembleDetail_maps_result_fixture_leg_and_never_exposes_winner_property()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(1, 1),
                extraTimePlayed: true,
                penaltyShootoutScore: new PenaltyShootoutScore(5, 4)),
            _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var detail = MatchReadAssembler.AssembleDetail(match, competition, stage);

        detail.MatchId.Should().Be(match.Id.Value);
        detail.CompetitionId.Should().Be(competition.Id.Value);
        detail.StageId.Should().Be(stage.Id.Value);
        detail.Home.DisplayName.Should().Be("Home");
        detail.Away.DisplayName.Should().Be("Away");
        detail.FixtureId.Should().Be(fixture.Id.Value);
        detail.LegIndex.Should().Be(1);
        detail.Result.Should().NotBeNull();
        detail.Result!.Type.Should().Be(ResultType.Played);
        detail.Result.HomeGoals.Should().Be(1);
        detail.Result.AwayGoals.Should().Be(1);
        detail.Result.ExtraTimePlayed.Should().BeTrue();
        detail.Result.Shootout.Should().Be(new MatchScoreDto(5, 4));
        typeof(MatchDetailDto).GetProperty("Winner").Should().BeNull();
    }

    [Fact]
    public void AssembleDetail_when_no_result_leaves_result_null()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var match = Match.Create(competition.Id, StageId.New(), home.Id, away.Id, _clock);

        var detail = MatchReadAssembler.AssembleDetail(match, competition, stage: null);

        detail.Result.Should().BeNull();
        detail.FixtureId.Should().BeNull();
        detail.LegIndex.Should().BeNull();
        detail.ScheduledAt.Should().BeNull();
        detail.ResourceId.Should().BeNull();
        detail.HasObservedLive.Should().BeFalse();
        detail.RunningScore.Should().BeNull();
        detail.DeclaredParticipations.Should().BeEmpty();
        detail.RecordedGoals.Should().BeEmpty();
        detail.RecordedSubstitutions.Should().BeEmpty();
        detail.RecordedDisciplinaryEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssembleDetail_maps_sheet_running_score_and_recorded_facts_with_display_names()
    {
        var standard = SampleRegulations.Standard();
        var regulation = new Regulation(
            standard.EntryRules,
            standard.MatchRules,
            standard.StandingRules,
            new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));
        var competition = CreateCompetition.Execute("Facts Cup", regulation, _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var dupont = AddDeclaredMember.Execute(competition, home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var martin = AddDeclaredMember.Execute(competition, home.Id, "Martin", DeclaredMemberRole.Player, _clock);
        var awayPlayer = AddDeclaredMember.Execute(competition, away.Id, "Rival", DeclaredMemberRole.Player, _clock);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(dupont.Id, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 9);
        match.AddDeclaredParticipation(martin.Id, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(awayPlayer.Id, Side.Away, CompositionStatus.Starter, _clock);
        match.Start(_clock);
        SetRunningScore.Execute(match, 2, 0, _clock);

        var goal = RecordGoal.Execute(match, dupont.Id, Side.Home, _clock, assisterMemberId: martin.Id);
        var substitution = RecordSubstitution.Execute(match, dupont.Id, martin.Id, Side.Home, _clock);
        var disciplinary = RecordDisciplinaryEvent.Execute(
            match, competition, awayPlayer.Id, DisciplinaryType.Yellow, _clock);

        var detail = MatchReadAssembler.AssembleDetail(match, competition, stage);

        detail.HasObservedLive.Should().BeTrue();
        detail.RunningScore.Should().Be(new MatchScoreDto(2, 0));

        detail.DeclaredParticipations.Should().HaveCount(3);
        detail.DeclaredParticipations.Should().ContainEquivalentOf(new DeclaredParticipationDto(
            dupont.Id.Value, "Dupont", Side.Home, CompositionStatus.Starter, 9));
        detail.DeclaredParticipations.Should().ContainEquivalentOf(new DeclaredParticipationDto(
            martin.Id.Value, "Martin", Side.Home, CompositionStatus.Bench, null));
        detail.DeclaredParticipations.Should().ContainEquivalentOf(new DeclaredParticipationDto(
            awayPlayer.Id.Value, "Rival", Side.Away, CompositionStatus.Starter, null));

        detail.RecordedGoals.Should().ContainSingle().Which.Should().BeEquivalentTo(new RecordedGoalDto(
            goal.Id.Value,
            dupont.Id.Value,
            "Dupont",
            Side.Home,
            martin.Id.Value,
            "Martin",
            IsOwnGoal: false));

        detail.RecordedSubstitutions.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedSubstitutionDto(
                substitution.Id.Value,
                Side.Home,
                dupont.Id.Value,
                "Dupont",
                martin.Id.Value,
                "Martin"));

        detail.RecordedDisciplinaryEvents.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecordedDisciplinaryEventDto(
                disciplinary.Id.Value,
                awayPlayer.Id.Value,
                "Rival",
                DisciplinaryType.Yellow));
    }
}
