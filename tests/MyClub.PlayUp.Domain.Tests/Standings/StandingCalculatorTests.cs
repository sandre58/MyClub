// -----------------------------------------------------------------------
// <copyright file="StandingCalculatorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Standings;

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
        // A and B both on 4 points; B has better global GD (+2 vs -3) but A wins the H2H mini-table.
        var rules = Rules(RankingCriterion.Points, RankingCriterion.HeadToHead);
        var matches = new[]
        {
            new StandingMatch(_a, _c, 1, 5),
            new StandingMatch(_b, _c, 3, 0),
            new StandingMatch(_a, _b, 1, 0),
            new StandingMatch(_b, _a, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b, _c], matches, rules);

        standing.Find(_a)!.Points.Should().Be(4);
        standing.Find(_b)!.Points.Should().Be(4);
        standing.Find(_a)!.GoalDifference.Should().BeLessThan(standing.Find(_b)!.GoalDifference);
        standing.Rows.Select(r => r.EntryId).Should().Equal(_a, _b, _c);
    }

    [Fact]
    public void Calculate_all_filter_counts_both_sides_of_each_match()
    {
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 1),
            new StandingMatch(_b, _a, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b], matches, rules);

        standing.Find(_a)!.Played.Should().Be(2);
        standing.Find(_a)!.Points.Should().Be(4);
        standing.Find(_a)!.GoalsFor.Should().Be(2);
        standing.Find(_a)!.GoalsAgainst.Should().Be(1);
        standing.Find(_b)!.Played.Should().Be(2);
        standing.Find(_b)!.Points.Should().Be(1);
        standing.Find(_b)!.GoalsFor.Should().Be(1);
        standing.Find(_b)!.GoalsAgainst.Should().Be(2);
        standing.Rows.Select(r => r.EntryId).Should().Equal(_a, _b);
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
    public void Calculate_home_filter_head_to_head_uses_only_home_legs()
    {
        // A and B both 4 home points. Mutual home legs: A beats B 3-1, B beats A 1-0.
        // Home-filtered H2H: both 3 pts, A better H2H GD (+2 vs +1) → A first.
        // Unfiltered H2H would credit away sides and rank B first (4 H2H pts vs 3).
        var rules = Rules(RankingCriterion.Points, RankingCriterion.HeadToHead);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 3, 1),
            new StandingMatch(_b, _a, 1, 0),
            new StandingMatch(_a, _c, 0, 0),
            new StandingMatch(_b, _c, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b, _c], matches, rules, MatchFilter.Home);

        standing.Find(_a)!.Points.Should().Be(4);
        standing.Find(_b)!.Points.Should().Be(4);
        standing.Rows[0].EntryId.Should().Be(_a);
        standing.Rows[1].EntryId.Should().Be(_b);
    }

    [Fact]
    public void Calculate_away_filter_head_to_head_uses_only_away_legs()
    {
        // Symmetric to home: away-only H2H must ignore home legs among tied teams.
        var rules = Rules(RankingCriterion.Points, RankingCriterion.HeadToHead);
        var matches = new[]
        {
            new StandingMatch(_b, _a, 1, 3),
            new StandingMatch(_a, _b, 0, 1),
            new StandingMatch(_c, _a, 0, 0),
            new StandingMatch(_c, _b, 0, 0)
        };

        var standing = StandingCalculator.Calculate([_a, _b, _c], matches, rules, MatchFilter.Away);

        standing.Find(_a)!.Points.Should().Be(4);
        standing.Find(_b)!.Points.Should().Be(4);
        standing.Rows[0].EntryId.Should().Be(_a);
        standing.Rows[1].EntryId.Should().Be(_b);
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

    [Fact]
    public void Calculate_penalty_changes_order_by_net_points()
    {
        // Case 1: A 6 / B 3 / C 0 sportifs ; A −4 → B first on points alone.
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference, RankingCriterion.GoalsFor);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 0),
            new StandingMatch(_a, _c, 1, 0),
            new StandingMatch(_b, _c, 1, 0)
        };

        var standing = StandingCalculator.Calculate(
            [_a, _b, _c],
            matches,
            rules,
            penalties: [new StandingPenalty(_a, 4)]);

        standing.Rows.Select(r => r.EntryId).Should().Equal(_b, _a, _c);
        standing.Find(_a)!.Points.Should().Be(2);
        standing.Find(_b)!.Points.Should().Be(3);
        standing.Find(_a)!.Wins.Should().Be(2);
    }

    [Fact]
    public void Calculate_sums_multiple_penalties_for_same_entry()
    {
        // Case 2: A has 7 sport points (2W 1D) then −1 and −3 → 3.
        var rules = Rules(RankingCriterion.Points);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 1, 0),
            new StandingMatch(_a, _c, 2, 0),
            new StandingMatch(_a, _b, 1, 1)
        };

        var standing = StandingCalculator.Calculate(
            [_a, _b, _c],
            matches,
            rules,
            penalties: [new StandingPenalty(_a, 1), new StandingPenalty(_a, 3)]);

        standing.Find(_a)!.Points.Should().Be(3);
    }

    [Fact]
    public void Calculate_allows_negative_net_points()
    {
        // Case 3: no matches, −3 → Points = −3.
        var rules = Rules(RankingCriterion.Points);

        var standing = StandingCalculator.Calculate(
            [_a, _b],
            [],
            rules,
            penalties: [new StandingPenalty(_a, 3)]);

        standing.Find(_a)!.Points.Should().Be(-3);
        standing.Find(_b)!.Points.Should().Be(0);
        standing.Rows[0].EntryId.Should().Be(_b);
    }

    [Fact]
    public void Calculate_penalties_do_not_affect_head_to_head_mini_table()
    {
        // Case 4: A and B both 4 sport points; H2H favors A. After A −1, global Points ranks B first.
        // A drops to 3 (tied with C who beat A); H2H among A/C then ranks C ahead — proving the
        // global Points step applied the penalty before any H2H mini-table.
        var rules = Rules(RankingCriterion.Points, RankingCriterion.HeadToHead);
        var matches = new[]
        {
            new StandingMatch(_a, _c, 1, 5),
            new StandingMatch(_b, _c, 3, 0),
            new StandingMatch(_a, _b, 1, 0),
            new StandingMatch(_b, _a, 0, 0)
        };

        var without = StandingCalculator.Calculate([_a, _b, _c], matches, rules);
        without.Find(_a)!.Points.Should().Be(4);
        without.Find(_b)!.Points.Should().Be(4);
        without.Rows.Select(r => r.EntryId).Should().Equal(_a, _b, _c);

        var withPenalty = StandingCalculator.Calculate(
            [_a, _b, _c],
            matches,
            rules,
            penalties: [new StandingPenalty(_a, 1)]);

        withPenalty.Find(_a)!.Points.Should().Be(3);
        withPenalty.Find(_b)!.Points.Should().Be(4);
        withPenalty.Find(_c)!.Points.Should().Be(3);
        withPenalty.Rows.Select(r => r.EntryId).Should().Equal(_b, _c, _a);
    }

    [Fact]
    public void Calculate_home_filter_still_applies_global_penalty()
    {
        // Case 5: home-only points for A = 3; global penalty −3 → 0.
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 0),
            new StandingMatch(_b, _a, 3, 0)
        };

        var standing = StandingCalculator.Calculate(
            [_a, _b],
            matches,
            rules,
            MatchFilter.Home,
            [new StandingPenalty(_a, 3)]);

        standing.Find(_a)!.Played.Should().Be(1);
        standing.Find(_a)!.Points.Should().Be(0);
        standing.Find(_b)!.Points.Should().Be(3);
    }

    [Fact]
    public void Calculate_null_or_empty_penalties_match_legacy_behavior()
    {
        var rules = Rules(RankingCriterion.Points, RankingCriterion.GoalDifference);
        var matches = new[]
        {
            new StandingMatch(_a, _b, 2, 0),
            new StandingMatch(_a, _c, 1, 0),
            new StandingMatch(_b, _c, 3, 1)
        };

        var legacy = StandingCalculator.Calculate([_a, _b, _c], matches, rules);
        var withNull = StandingCalculator.Calculate([_a, _b, _c], matches, rules, penalties: null);
        var withEmpty = StandingCalculator.Calculate([_a, _b, _c], matches, rules, penalties: []);

        withNull.Rows.Select(r => (r.EntryId, r.Points)).Should().Equal(legacy.Rows.Select(r => (r.EntryId, r.Points)));
        withEmpty.Rows.Select(r => (r.EntryId, r.Points)).Should().Equal(legacy.Rows.Select(r => (r.EntryId, r.Points)));
    }

    private static StandingRules Rules(params RankingCriterion[] criteria) =>
        new(new PointsPolicy(3, 1, 0), criteria);
}
