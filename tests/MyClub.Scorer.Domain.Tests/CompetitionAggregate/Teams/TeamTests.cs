// -----------------------------------------------------------------------
// <copyright file="TeamTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using AutoFixture;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate.Teams;

public class TeamTests
{
    [Fact]
    public void Create_ShouldInitializeTeamWithNameAndShortName()
    {
        var name = new Fixture().Create<string>();
        var shortName = new Fixture().Create<string>();

        var team = Team.Create(name, shortName);

        team.DisplayName.Name.Should().Be(name);
        team.DisplayName.ShortName.Should().Be(shortName);
        team.Players.Should().BeEmpty();
        team.Staff.Should().BeEmpty();
    }

    [Fact]
    public void AddPlayer_ShouldAddPlayer()
    {
        var team = Team.Create("Team1");
        var player = Player.Create("John", "Doe");

        var result = team.AddPlayer(player);

        result.IsSuccess.Should().BeTrue();
        team.Players.Should().Contain(player);
    }

    [Fact]
    public void AddPlayer_ShouldNotAddDuplicatePlayer()
    {
        var team = Team.Create("Team1");
        var player = Player.Create("John", "Doe");

        team.AddPlayer(player);
        var result = team.AddPlayer(player);

        result.IsFailure.Should().BeTrue();
        team.Players.Should().HaveCount(1);
    }

    [Fact]
    public void AddPlayer_ByName_ShouldAddPlayer()
    {
        var team = Team.Create("Team1");

        var result = team.AddPlayer("Jane", "Smith");

        result.IsSuccess.Should().BeTrue();
        team.Players.Should().ContainSingle(static p => p.FirstName == "Jane" && p.LastName == "Smith");
    }

    [Fact]
    public void RemovePlayer_ShouldRemovePlayer()
    {
        var team = Team.Create("Team1");
        var player = Player.Create("John", "Doe");

        team.AddPlayer(player);
        var removed = team.RemovePlayer(player);

        removed.Should().BeTrue();
        team.Players.Should().BeEmpty();
    }

    [Fact]
    public void RemovePlayers_ShouldRemoveMultiplePlayers()
    {
        var team = Team.Create("Team1");
        var player1 = Player.Create("John", "Doe");
        var player2 = Player.Create("Jane", "Smith");

        team.AddPlayer(player1);
        team.AddPlayer(player2);

        var removedCount = team.RemovePlayers([player1, player2]);

        removedCount.Should().Be(2);
        team.Players.Should().BeEmpty();
    }

    [Fact]
    public void AddManager_ShouldAddManager()
    {
        var team = Team.Create("Team1");
        var manager = Manager.Create("Coach", "One");

        var result = team.AddManager(manager);

        result.IsSuccess.Should().BeTrue();
        team.Staff.Should().Contain(manager);
    }

    [Fact]
    public void AddManager_ShouldNotAddDuplicateManager()
    {
        var team = Team.Create("Team1");
        var manager = Manager.Create("Coach", "One");

        team.AddManager(manager);
        var result = team.AddManager(manager);

        result.IsFailure.Should().BeTrue();
        team.Staff.Should().HaveCount(1);
    }

    [Fact]
    public void AddManager_ByName_ShouldAddManager()
    {
        var team = Team.Create("Team1");

        var result = team.AddManager("Coach", "Two");

        result.IsSuccess.Should().BeTrue();
        team.Staff.Should().ContainSingle(static m => m.FirstName == "Coach" && m.LastName == "Two");
    }

    [Fact]
    public void RemoveManager_ShouldRemoveManager()
    {
        var team = Team.Create("Team1");
        var manager = Manager.Create("Coach", "One");

        team.AddManager(manager);
        var removed = team.RemoveManager(manager);

        removed.Should().BeTrue();
        team.Staff.Should().BeEmpty();
    }

    [Fact]
    public void RemoveManagers_ShouldRemoveMultipleManagers()
    {
        var team = Team.Create("Team1");
        var manager1 = Manager.Create("Coach", "One");
        var manager2 = Manager.Create("Coach", "Two");

        team.AddManager(manager1);
        team.AddManager(manager2);

        var removedCount = team.RemoveManagers([manager1, manager2]);

        removedCount.Should().Be(2);
        team.Staff.Should().BeEmpty();
    }
}
