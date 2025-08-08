// -----------------------------------------------------------------------
// <copyright file="LeagueTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate;

public class LeagueTests
{
    [Fact]
    public void Create_ShouldInitializeLeagueWithDefaultValues()
    {
        const string name = "League 1";
        var league = League.Create(name);

        league.DisplayName.Name.Should().Be(name);
        league.MatchFormat.Should().Be(MatchFormat.Default);
        league.MatchRules.Should().Be(MatchRules.Default);
        league.StandingRules.Should().Be(StandingRuleSet.Default);
        league.Labels.Should().HaveCount(0);
        league.Type.Should().Be(CompetitionType.League);
        league.Matchdays.Should().BeEmpty();
        league.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMatchday_ShouldRemoveMatchday()
    {
        var league = League.Create("League 1");
        var matchdayId = MatchdayId.New();

        league.AddMatchday(matchdayId);
        var removed = league.RemoveMatchday(matchdayId);

        removed.Should().BeTrue();
        league.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllMatchdays()
    {
        var league = League.Create("Ligue 1");
        league.AddMatchday(MatchdayId.New());
        league.AddMatchday(MatchdayId.New());

        league.Clear();

        league.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void AddPenalty_ShouldAddOrUpdatePenaltyPoints()
    {
        var league = League.Create("Ligue 1");
        var teamId = new TeamId(Guid.NewGuid());

        league.AddPenalty(teamId, 3);

        league.PenaltyPoints.Should().ContainKey(teamId);
        league.PenaltyPoints[teamId].Should().Be(3);

        league.AddPenalty(teamId, 5);

        league.PenaltyPoints[teamId].Should().Be(5);
    }

    [Fact]
    public void RemovePenalty_ShouldRemovePenaltyPoints()
    {
        var league = League.Create("Ligue 1");
        var teamId = new TeamId(Guid.NewGuid());

        league.AddPenalty(teamId, 2);
        var removed = league.RemovePenalty(teamId);

        removed.Should().BeTrue();
        league.PenaltyPoints.Should().NotContainKey(teamId);
    }

    [Fact]
    public void ClearPenaltyPoints_ShouldRemoveAllPenaltyPoints()
    {
        var league = League.Create("Ligue 1");
        var teamId1 = new TeamId(Guid.NewGuid());
        var teamId2 = new TeamId(Guid.NewGuid());

        league.AddPenalty(teamId1, 1);
        league.AddPenalty(teamId2, 2);

        league.ClearPenaltyPoints();

        league.PenaltyPoints.Should().BeEmpty();
    }
}
