// -----------------------------------------------------------------------
// <copyright file="RoundTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AutoFixture;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Teams;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate;

public class RoundTests : TestBase
{
    [Fact]
    public void Create_ShouldInitializeRoundWithCorrectValues()
    {
        var format = Fixture.Create<SingleFormat>();
        var name = "Quarter Finals";
        var shortName = "QF";
        var rules = new MatchRules([CardColor.Red, CardColor.Yellow]);
        var round = Round.Create(null, format, name, shortName, rules, true);

        round.DisplayName.Name.Should().Be(name);
        round.DisplayName.ShortName.Should().Be(shortName);
        round.Format.Should().Be(format);
        round.CustomMatchRules.Should().Be(rules);
        round.IsConsolation.Should().BeTrue();
        round.Teams.Should().BeEmpty();
        round.Fixtures.Should().BeEmpty();
        round.Stages.Should().BeEmpty();
    }

    [Fact]
    public void AddTeam_ShouldAddTeam_WhenNotExists()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team = TeamId.New().ToReference();

        var result = round.AddTeam(team);

        result.IsSuccess.Should().BeTrue();
        round.Teams.Should().ContainSingle().Which.Should().Be(team);
    }

    [Fact]
    public void AddTeam_ShouldFail_WhenTeamAlreadyExists()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team = TeamId.New().ToReference();
        round.AddTeam(team);

        var result = round.AddTeam(team);

        result.IsFailure.Should().BeTrue();
        round.Teams.Should().ContainSingle();
    }

    [Fact]
    public void RemoveTeam_ShouldRemoveTeamAndRelatedFixtures()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team1 = TeamId.New().ToReference();
        var team2 = TeamId.New().ToReference();
        round.AddTeam(team1);
        round.AddTeam(team2);
        round.AddFixture(team1, team2);

        var removed = round.RemoveTeam(team1);

        removed.Should().BeTrue();
        round.Teams.Should().ContainSingle().Which.Should().Be(team2);
        round.Fixtures.Should().BeEmpty();
    }

    [Fact]
    public void AddFixture_ShouldAddFixture_WhenNotExists()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team1 = TeamId.New().ToReference();
        var team2 = TeamId.New().ToReference();

        var result = round.AddFixture(team1, team2);

        result.IsSuccess.Should().BeTrue();
        round.Fixtures.Should().ContainSingle();
    }

    [Fact]
    public void AddFixture_ShouldFail_WhenFixtureAlreadyExists()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team1 = TeamId.New().ToReference();
        var team2 = TeamId.New().ToReference();
        round.AddFixture(team1, team2);

        var result = round.AddFixture(team1, team2);

        result.IsFailure.Should().BeTrue();
        round.Fixtures.Should().ContainSingle();
    }

    [Fact]
    public void RemoveFixture_ShouldRemoveFixture()
    {
        var format = Fixture.Create<SingleFormat>();
        var round = Round.Create(null, format, "R", "R");
        var team1 = TeamId.New().ToReference();
        var team2 = TeamId.New().ToReference();
        var fixtureResult = round.AddFixture(team1, team2);

        var removed = round.RemoveFixture(fixtureResult.Value);

        removed.Should().BeTrue();
        round.Fixtures.Should().BeEmpty();
    }
}
