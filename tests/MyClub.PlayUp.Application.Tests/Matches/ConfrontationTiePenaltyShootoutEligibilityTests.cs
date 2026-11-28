// -----------------------------------------------------------------------
// <copyright file="ConfrontationTiePenaltyShootoutEligibilityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using Match = MyClub.PlayUp.Domain.Matches.Match;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class ConfrontationTiePenaltyShootoutEligibilityTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 28, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task IsEligible_true_on_final_leg_when_aggregate_still_levelAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_false_when_play_score_is_equalAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 0,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_when_shootout_absentAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(ResultType.Played, HomeGoals: 1, AwayGoals: 0);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_on_first_legAsync()
    {
        var (stage, first, _) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            first,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_when_first_leg_not_finishedAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_when_away_goals_break_aggregateAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true, awayGoals: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_when_penalty_shootout_rule_absentAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: false);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_on_single_leg_cup_async()
    {
        var (stage, match) = CreateSingleLegCup(withShootout: true);
        match.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            match,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_on_championship_match_without_fixtureAsync()
    {
        var stage = CreateRunningChampionshipStage();
        var match = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Played,
            HomeGoals: 2,
            AwayGoals: 1,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            match,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_false_for_forfeit_with_shootout_countsAsync()
    {
        var (stage, first, second) = CreateTwoLegCup(withShootout: true);
        first.Start(_clock);
        first.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        second.Start(_clock);

        var entry = new MatchResultEntry(
            ResultType.Forfeit,
            HomeGoals: 0,
            AwayGoals: 3,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        var matches = RepoReturning(first);
        var eligible = await ConfrontationTiePenaltyShootoutEligibility.IsEligibleAsync(
            stage,
            second,
            entry,
            matches.Object,
            CancellationToken.None);

        eligible.Should().BeFalse();
    }

    private static Mock<IMatchRepository> RepoReturning(Match firstLeg)
    {
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        matches
            .Setup(repo => repo.GetByIdReadOnlyAsync(firstLeg.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstLeg);
        return matches;
    }

    private (Stage Stage, Match First, Match Second) CreateTwoLegCup(
        bool withShootout,
        bool awayGoals = false)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var tieFormat = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: awayGoals ? new AwayGoalsRule() : null,
            penaltyShootoutRule: withShootout ? new PenaltyShootoutRule() : null);
        stage.AddRound("R1", tieFormat, _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "A", "B", "P1");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
        return (stage, first, second);
    }

    private (Stage Stage, Match Match) CreateSingleLegCup(bool withShootout)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var tieFormat = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: withShootout ? new PenaltyShootoutRule() : null);
        var round = stage.AddRound("Final", tieFormat, _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(round.Id, _clock, "A", "B", "P1");
        var match = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        return (stage, match);
    }

    private Stage CreateRunningChampionshipStage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("League"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}