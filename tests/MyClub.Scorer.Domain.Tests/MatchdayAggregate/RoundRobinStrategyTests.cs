// -----------------------------------------------------------------------
// <copyright file="RoundRobinStrategyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MyClub.Scorer.Domain.MatchdayAggregate.Services;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.MatchdayAggregate;

public class RoundRobinStrategyTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetProperties()
    {
        var strategy = RoundRobinStrategy.Default;

        strategy.MatchesPerPair.Should().Be(2);
        strategy.InvertTeamsByStage.Should().HaveCount(2);
        strategy.InvertTeamsByStage.Should().BeEquivalentTo([false, true]);
    }

    [Fact]
    public void CustomConstructor_ShouldSetProperties()
    {
        var strategy = new RoundRobinStrategy(3, [false, true, true]);

        strategy.MatchesPerPair.Should().Be(3);
        strategy.InvertTeamsByStage.Should().BeEquivalentTo([false, true, true]);
    }

    [Theory]
    [InlineData(4, 2, 6)] // 4 teams, 2 matches per pair, 6 matchdays
    [InlineData(5, 1, 5)] // 5 teams, 1 match per pair, 5 matchdays
    [InlineData(6, 2, 10)] // 6 teams, 2 matches per pair, 10 matchdays
    public void GetMatchdayCount_ShouldReturnExpected(int teamCount, int matchesPerPair, int expected)
    {
        var strategy = new RoundRobinStrategy(matchesPerPair);

        strategy.GetMatchdayCount(teamCount).Should().Be(expected);
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 3)]
    public void GetMaxMatchesPerMatchday_ShouldReturnExpected(int teamCount, int expected)
    {
        var strategy = RoundRobinStrategy.Default;

        strategy.GetMaxMatchesPerMatchday(teamCount).Should().Be(expected);
    }

    [Fact]
    public void GenerateSchedule_ShouldCreateCorrectNumberOfMatchdays_AndFixtures()
    {
        var teams = new[]
        {
            TeamId.New().ToReference(),
            TeamId.New().ToReference(),
            TeamId.New().ToReference(),
            TeamId.New().ToReference()
        };

        var strategy = new RoundRobinStrategy(2);
        var schedule = strategy.GenerateSchedule(teams).ToList();

        // For 4 teams, 2 matches per pair: (n-1) * matchesPerPair = 3*2 = 6 matchdays
        schedule.Should().HaveCount(6);

        foreach (var matchday in schedule)
        {
            matchday.Fixtures.Should().HaveCount(2); // 4 teams => 2 matches per matchday
            var allTeams = matchday.Fixtures.SelectMany(f => new[] { f.HomeTeam, f.AwayTeam }).ToList();
            allTeams.Should().OnlyHaveUniqueItems();
        }

        // Check that all pairs are present twice (home/away)
        var pairs = new HashSet<(TeamReference, TeamReference)>();
        foreach (var matchday in schedule)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                pairs.Add((fixture.HomeTeam, fixture.AwayTeam));
            }
        }

        // There should be 12 unique matches (each pair played twice)
        pairs.Count.Should().Be(12);
    }

    [Fact]
    public void GenerateSchedule_ShouldWorkWithOddNumberOfTeams()
    {
        var teams = new[]
        {
            TeamId.New().ToReference(),
            TeamId.New().ToReference(),
            TeamId.New().ToReference(),
            TeamId.New().ToReference(),
            TeamId.New().ToReference()
        };

        var strategy = new RoundRobinStrategy(1);
        var schedule = strategy.GenerateSchedule(teams).ToList();

        // For 5 teams, 1 match per pair: (n) = 5 matchdays
        schedule.Should().HaveCount(5);

        foreach (var matchday in schedule)
        {
            // For odd teams, one team has a bye each matchday
            matchday.Fixtures.Count.Should().Be(2);
        }
    }
}
