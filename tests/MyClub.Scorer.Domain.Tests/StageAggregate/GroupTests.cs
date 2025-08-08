// -----------------------------------------------------------------------
// <copyright file="GroupTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class GroupTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateGroup()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()), new ConcreteTeamReference(TeamId.New()) };
        const string name = "Group A";
        const string shortName = "A";

        // Act
        var group = Group.Create(groupStage, teams, name, shortName);

        // Assert
        group.Should().NotBeNull();
        group.Id.Should().NotBeNull();
        group.DisplayName.Name.Should().Be(name);
        group.DisplayName.ShortName.Should().Be(shortName);
        group.Teams.Should().HaveCount(2);
        group.Teams.Should().Contain(teams);
        group.StandingRules.Should().Be(groupStage.StandingRules);
        group.Labels.Should().BeSameAs(groupStage.Labels);
        group.Matchdays.Should().BeEquivalentTo(groupStage.Matchdays);
    }

    [Fact]
    public void Create_WithoutShortName_ShouldCreateGroupWithNullShortName()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        const string name = "Group B";

        // Act
        var group = Group.Create(groupStage, teams, name);

        // Assert
        group.Should().NotBeNull();
        group.DisplayName.Name.Should().Be(name);
        group.DisplayName.ShortName.Should().Be("GB");
    }

    [Fact]
    public void Create_WithEmptyTeams_ShouldCreateGroupWithEmptyTeamsList()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = Array.Empty<TeamReference>();
        const string name = "Group C";

        // Act
        var group = Group.Create(groupStage, teams, name);

        // Assert
        group.Should().NotBeNull();
        group.Teams.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => Group.Create(groupStage, teams, string.Empty));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => Group.Create(groupStage, teams, null!));
    }

    [Fact]
    public void StandingRules_ShouldReturnGroupStageStandingRules()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var customRules = StandingRuleSet.Default;
        groupStage.StandingRules = customRules;
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act & Assert
        group.StandingRules.Should().Be(customRules);
        group.StandingRules.Should().Be(groupStage.StandingRules);
    }

    [Fact]
    public void Labels_ShouldReturnGroupStageLabels()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var customLabels = new StandingLabels();
        groupStage.Labels = customLabels;
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act & Assert
        group.Labels.Should().BeSameAs(customLabels);
        group.Labels.Should().BeSameAs(groupStage.Labels);
    }

    [Fact]
    public void Matchdays_ShouldReturnGroupStageMatchdays()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var matchdayId = MatchdayId.New();
        groupStage.AddMatchday(matchdayId);
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act & Assert
        group.Matchdays.Should().Contain(matchdayId);
        group.Matchdays.Should().BeEquivalentTo(groupStage.Matchdays);
    }

    [Fact]
    public void PenaltyPoints_ShouldReturnOnlyTeamsInGroup()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamInGroup = TeamId.New();
        var teamOutOfGroup = TeamId.New();
        var teamRefInGroup = new ConcreteTeamReference(teamInGroup);

        groupStage.AddPenalty(teamInGroup, 5);
        groupStage.AddPenalty(teamOutOfGroup, 3);

        var teams = new[] { teamRefInGroup };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act
        var penaltyPoints = group.PenaltyPoints;

        // Assert
        penaltyPoints.Should().HaveCount(1);
        penaltyPoints.Should().ContainKey(teamInGroup);
        penaltyPoints.Should().NotContainKey(teamOutOfGroup);
        penaltyPoints[teamInGroup].Should().Be(5);
    }

    [Fact]
    public void PenaltyPoints_WithNoTeamsInGroup_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamOutOfGroup = TeamId.New();
        groupStage.AddPenalty(teamOutOfGroup, 3);

        var teams = Array.Empty<TeamReference>();
        var group = Group.Create(groupStage, teams, "Group A");

        // Act
        var penaltyPoints = group.PenaltyPoints;

        // Assert
        penaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void IsSimilar_WithSameDisplayName_ShouldReturnTrue()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group1 = Group.Create(groupStage, teams, "Group A", "A");
        var group2 = Group.Create(groupStage, teams, "Group A", "A");

        // Act
        var result = group1.IsSimilar(group2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_WithDifferentDisplayName_ShouldReturnFalse()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group1 = Group.Create(groupStage, teams, "Group A", "A");
        var group2 = Group.Create(groupStage, teams, "Group B", "B");

        // Act
        var result = group1.IsSimilar(group2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act
        var result = group.IsSimilar(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A", "A");

        // Act
        var result = group.ToString();

        // Assert
        result.Should().Be("Group A");
    }

    [Fact]
    public void DisplayName_ShouldBeSettable()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");
        var newDisplayName = new MyClub.Shared.Domain.ValueObjects.DisplayName("Group B", "B");

        // Act
        group.DisplayName = newDisplayName;

        // Assert
        group.DisplayName.Should().Be(newDisplayName);
        group.DisplayName.Name.Should().Be("Group B");
        group.DisplayName.ShortName.Should().Be("B");
    }

    [Fact]
    public void Teams_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()), new ConcreteTeamReference(TeamId.New()) };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act
        var groupTeams = group.Teams;

        // Assert
        groupTeams.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyCollection<TeamReference>>();
        groupTeams.Should().HaveCount(2);
        groupTeams.Should().Contain(teams);
    }

    [Fact]
    public void PenaltyPoints_ShouldBeReadOnlyDictionary()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId = TeamId.New();
        var teamRef = new ConcreteTeamReference(teamId);
        groupStage.AddPenalty(teamId, 5);
        var teams = new[] { teamRef };
        var group = Group.Create(groupStage, teams, "Group A");

        // Act
        var penaltyPoints = group.PenaltyPoints;

        // Assert
        penaltyPoints.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyDictionary<TeamId, int>>();
        penaltyPoints.Should().HaveCount(1);
        penaltyPoints.Should().ContainKey(teamId);
        penaltyPoints[teamId].Should().Be(5);
    }

    [Fact]
    public void UniqueIds_ShouldBeGeneratedForDifferentInstances()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };

        // Act
        var group1 = Group.Create(groupStage, teams, "Group A");
        var group2 = Group.Create(groupStage, teams, "Group B");

        // Assert
        group1.Id.Should().NotBe(group2.Id);
        group1.Id.Value.Should().NotBe(group2.Id.Value);
    }

    [Fact]
    public void Teams_WithDuplicateTeams_ShouldContainAllTeams()
    {
        // Arrange
        var groupStage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());
        var teams = new[] { teamRef, teamRef }; // Duplicate team

        // Act
        var group = Group.Create(groupStage, teams, "Group A");

        // Assert
        group.Teams.Should().HaveCount(2);
        group.Teams.Should().Contain(teamRef);
    }
}
