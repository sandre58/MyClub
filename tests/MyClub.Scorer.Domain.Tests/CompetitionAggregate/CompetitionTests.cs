// -----------------------------------------------------------------------
// <copyright file="CompetitionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate;

public class CompetitionTests : TestBase
{
    private string RandomName() => Create<string>();

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var id = CompetitionId.New();
        var name = RandomName();
        var shortName = Create<string>();
        var format = MatchFormat.Default;
        var rules = MatchRules.Default;

        var competition = new DummyCompetition(id, name, shortName, format, rules);

        competition.Id.Should().Be(id);
        competition.DisplayName.Name.Should().Be(name);
        competition.DisplayName.ShortName.Should().Be(shortName);
        competition.MatchFormat.Should().Be(format);
        competition.MatchRules.Should().Be(rules);
        competition.Teams.Should().BeEmpty();
        competition.Stadiums.Should().BeEmpty();
    }

    [Fact]
    public void AddTeam_ShouldAddTeam()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var team = Team.Create("Team1");

        var result = competition.AddTeam(team);

        result.IsSuccess.Should().BeTrue();
        competition.Teams.Should().Contain(team);
    }

    [Fact]
    public void AddTeam_ShouldNotAddDuplicateTeam()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var team = Team.Create("Team1");

        competition.AddTeam(team);
        var result = competition.AddTeam(team);

        result.IsFailure.Should().BeTrue();
        competition.Teams.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTeam_ShouldRemoveTeam()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var team = Team.Create("Team1");

        competition.AddTeam(team);
        var removed = competition.RemoveTeam(team);

        removed.Should().BeTrue();
        competition.Teams.Should().BeEmpty();
    }

    [Fact]
    public void HasSimilarTeams_ShouldReturnTrueIfNameExists()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var team = Team.Create("Team1");
        competition.AddTeam(team);

        competition.HasSimilarTeams("Team1").Should().BeTrue();
        competition.HasSimilarTeams("team1").Should().BeTrue();
        competition.HasSimilarTeams("Other").Should().BeFalse();
    }

    [Fact]
    public void AddStadium_ShouldAddStadium()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var stadium = Stadium.Create("Stadium1", Ground.Grass);

        var result = competition.AddStadium(stadium);

        result.IsSuccess.Should().BeTrue();
        competition.Stadiums.Should().Contain(stadium);
    }

    [Fact]
    public void AddStadium_ShouldNotAddDuplicateStadium()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var stadium = Stadium.Create("Stadium1", Ground.Grass);

        competition.AddStadium(stadium);
        var result = competition.AddStadium(stadium);

        result.IsFailure.Should().BeTrue();
        competition.Stadiums.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveStadium_ShouldRemoveStadium()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var stadium = Stadium.Create("Stadium1", Ground.Grass);

        competition.AddStadium(stadium);
        var removed = competition.RemoveStadium(stadium);

        removed.Should().BeTrue();
        competition.Stadiums.Should().BeEmpty();
    }

    [Fact]
    public void HasStadium_ShouldReturnTrueIfStadiumExists()
    {
        var competition = new DummyCompetition(CompetitionId.New(), RandomName(), null, MatchFormat.Default, MatchRules.Default);
        var stadium = Stadium.Create("Stadium1", Ground.Grass);
        competition.AddStadium(stadium);

        competition.HasStadium(stadium.Id).Should().BeTrue();
        competition.HasStadium(StadiumId.New()).Should().BeFalse();
    }
}

internal sealed class DummyCompetition(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules) : Competition(id, name, shortName, format, rules)
{
    public override CompetitionType Type => CompetitionType.League;
}
