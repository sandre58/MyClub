// -----------------------------------------------------------------------
// <copyright file="StandingMatchAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Standings;

public sealed class StandingMatchAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_ignores_shootout_so_equal_score_counts_as_draw()
    {
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(CompetitionId.New(), StageId.New(), home, away, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(2, 2),
                extraTimePlayed: true,
                new PenaltyShootoutScore(4, 3)),
            _clock);

        var snapshots = StandingMatchAssembler.Assemble([match]);
        var standing = StandingCalculator.Calculate(
            [home, away],
            snapshots,
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]));

        snapshots.Should().ContainSingle();
        snapshots[0].HomeGoals.Should().Be(2);
        snapshots[0].AwayGoals.Should().Be(2);
        standing.Rows.Should().HaveCount(2);
        standing.Rows.Should().OnlyContain(r => r.Draws == 1 && r.Wins == 0 && r.Losses == 0 && r.Points == 1);
    }

    [Fact]
    public void Assemble_excludes_a_finished_match_whose_side_is_unknown_from_standings_and_stats()
    {
        var home = EntryId.New();
        var away = EntryId.New();
        var played = Match.Create(CompetitionId.New(), StageId.New(), home, away, _clock);
        played.Start(_clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var deferred = new MyClub.PlayUp.Application.Abstractions.MatchSummaryRow(
            MatchId.New(),
            StageId.New(),
            MatchStatus.Finished,
            home,
            null,
            new MatchResult(ResultType.Played, new Score(5, 0)));

        var rules = new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]);
        var fromMatches = StandingMatchAssembler.Assemble([played]);
        var fromRows = StandingMatchAssembler.Assemble(
        [
            new MyClub.PlayUp.Application.Abstractions.MatchSummaryRow(
                played.Id,
                played.StageId,
                played.Status,
                played.HomeEntryId,
                played.AwayEntryId,
                played.Result),
            deferred
        ]);

        fromMatches.Should().ContainSingle();
        fromRows.Should().ContainSingle();
        fromRows[0].HomeGoals.Should().Be(1);
        var standing = StandingCalculator.Calculate([home, away], fromRows, rules);
        standing.Rows.Single(row => row.EntryId.Equals(home)).Played.Should().Be(1);
        standing.Rows.Single(row => row.EntryId.Equals(home)).GoalsFor.Should().Be(1);
        standing.Rows.Single(row => row.EntryId.Equals(away)).GoalsAgainst.Should().Be(1);
    }
}
