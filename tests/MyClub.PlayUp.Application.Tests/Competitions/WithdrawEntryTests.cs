// -----------------------------------------------------------------------
// <copyright file="WithdrawEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class WithdrawEntryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void WithdrawEntry_finishes_scheduled_match_as_forfeit_then_withdraws()
    {
        var (competition, stage, home, away, match) = CreateRunningWithMatch();

        WithdrawEntry.Execute(competition, home.Id, [stage], [match], _clock);

        competition.GetEntry(home.Id).Status.Should().Be(EntryStatus.Withdrawn);
        match.Status.Should().Be(MatchStatus.Finished);
        match.Result!.Type.Should().Be(ResultType.Forfeit);
        match.Result.Score.HomeGoals.Should().Be(0);
        match.Result.Score.AwayGoals.Should().Be(3);
        competition.GetEntry(away.Id).Status.Should().Be(EntryStatus.Active);
    }

    [Fact]
    public void WithdrawEntry_preserves_already_finished_match()
    {
        var (competition, stage, home, _, match) = CreateRunningWithMatch();
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        WithdrawEntry.Execute(competition, home.Id, [stage], [match], _clock);

        competition.GetEntry(home.Id).Status.Should().Be(EntryStatus.Withdrawn);
        match.Result!.Type.Should().Be(ResultType.Played);
        match.Result.Score.HomeGoals.Should().Be(2);
        match.Result.Score.AwayGoals.Should().Be(1);
    }

    [Fact]
    public void WithdrawEntries_forfeits_remaining_for_each_active_entry()
    {
        var (competition, stage, home, away, match) = CreateRunningWithMatch();

        WithdrawEntries.Execute(competition, [home.Id, away.Id], [stage], [match], _clock);

        competition.GetEntry(home.Id).Status.Should().Be(EntryStatus.Withdrawn);
        competition.GetEntry(away.Id).Status.Should().Be(EntryStatus.Withdrawn);
        match.Status.Should().Be(MatchStatus.Finished);
        match.Result!.Type.Should().Be(ResultType.Forfeit);
    }

    private (
        Domain.Competitions.Competition Competition,
        Stage Stage,
        Domain.Competitions.CompetitionEntry Home,
        Domain.Competitions.CompetitionEntry Away,
        Match Match) CreateRunningWithMatch()
    {
        var competition = CreateCompetition.Execute("Forfeit Cup", _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Main"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        return (competition, stage, home, away, match);
    }
}
