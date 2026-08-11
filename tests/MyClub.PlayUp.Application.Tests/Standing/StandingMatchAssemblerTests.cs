// -----------------------------------------------------------------------
// <copyright file="StandingMatchAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Standing;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Standing;

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
}
