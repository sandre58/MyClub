// -----------------------------------------------------------------------
// <copyright file="StandingCalculatorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Standing;

public sealed class StandingCalculatorTests
{
    private readonly EntryId _a = EntryId.New();
    private readonly EntryId _b = EntryId.New();
    private readonly EntryId _c = EntryId.New();

    [Fact]
    public void Calculate_orders_by_points_then_goal_difference()
    {
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference, RankingCriterion.GoalsFor);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 0),
            new StandingMatch(_a, _c, 1, 0),
            new StandingMatch(_b, _c, 3, 1)
        };

        var standing = StandingCalculator.Calculate([_a, _b, _c], matches, rules);

        standing.Rows.Select(r => r.EntryId).Should().Equal(_a, _b, _c);
        standing.Rows[0].Points.Should().Be(6);
        standing.Rows[1].Points.Should().Be(3);
        standing.Rows[2].Points.Should().Be(0);
    }

    [Fact]
    public void Calculate_uses_head_to_head_when_points_tied()
    {
        var rules = Rules(RankingCriterion.Points, RankingCriterion.HeadToHead, RankingCriterion.GoalDifference);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 1, 0),
            new StandingMatch(_a, _c, 0, 0),
            new StandingMatch(_b, _c, 2, 0),
            new StandingMatch(_b, _a, 0, 0),
            new StandingMatch(_c, _a, 1, 0),
            new StandingMatch(_c, _b, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b, _c], matches, rules);

        standing.Find(_a)!.Points.Should().Be(standing.Find(_b)!.Points);
        standing.Find(_a)!.Points.Should().Be(standing.Find(_c)!.Points);
        standing.EntryAt(1).Should().NotBeNull();
        standing.Rows.Should().HaveCount(3);
    }

    [Fact]
    public void Calculate_home_filter_only_counts_home_matches()
    {
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 0),
            new StandingMatch(_b, _a, 3, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b], matches, rules, MatchFilter.Home);

        standing.Find(_a)!.Played.Should().Be(1);
        standing.Find(_a)!.Points.Should().Be(3);
        standing.Find(_b)!.Played.Should().Be(1);
        standing.Find(_b)!.Points.Should().Be(3);
        standing.Find(_a)!.GoalsFor.Should().Be(2);
        standing.Find(_b)!.GoalsFor.Should().Be(3);
    }

    [Fact]
    public void Calculate_away_filter_only_counts_away_matches()
    {
        var rules = Rules(RankingCriterion.Points);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 1),
            new StandingMatch(_b, _a, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b], matches, rules, MatchFilter.Away);

        standing.Find(_a)!.Played.Should().Be(1);
        standing.Find(_a)!.Points.Should().Be(1);
        standing.Find(_b)!.Played.Should().Be(1);
        standing.Find(_b)!.Points.Should().Be(0);
    }

    [Fact]
    public void Calculate_rejects_empty_participants()
    {
        var act = () => StandingCalculator.Calculate(
            [],
            [],
            Rules(RankingCriterion.Points));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StandingErrorCodes.ParticipantsInvalid);
    }

    private static StandingRules Rules(params RankingCriterion[] criteria) =>
        new(new PointsPolicy(3, 1, 0), criteria);
}
