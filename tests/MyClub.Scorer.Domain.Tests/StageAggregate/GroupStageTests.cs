// -----------------------------------------------------------------------
// <copyright file="GroupStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class GroupStageTests
{
    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateGroupStage()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "Group Stage";

        // Act
        var stage = GroupStage.Create(ancestorStageId, name);

        // Assert
        stage.Should().NotBeNull();
        stage.Id.Should().NotBeNull();
        stage.AncestorStageId.Should().Be(ancestorStageId);
        stage.DisplayName.Name.Should().Be(name);
        stage.DisplayName.ShortName.Should().Be("GS");
        stage.Type.Should().Be(StageType.Groups);
        stage.IsConsolation.Should().BeFalse();
        stage.MatchFormat.Should().Be(MatchFormat.Default);
        stage.Rules.Should().Be(MatchRules.Default);
        stage.StandingRules.Should().Be(StandingRuleSet.Default);
        stage.Labels.Should().BeEmpty();
        stage.Groups.Should().BeEmpty();
        stage.Matchdays.Should().BeEmpty();
        stage.Teams.Should().BeEmpty();
        stage.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateGroupStageWithSpecifiedValues()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "Group Phase";
        const string shortName = "GP";
        var matchFormat = MatchFormat.Default;
        var rules = MatchRules.Default;
        var standingRules = StandingRuleSet.Default;
        var labels = new StandingLabels();
        const bool isConsolation = true;

        // Act
        var stage = GroupStage.Create(ancestorStageId, name, shortName, matchFormat, rules, standingRules, labels, isConsolation);

        // Assert
        stage.Should().NotBeNull();
        stage.AncestorStageId.Should().Be(ancestorStageId);
        stage.DisplayName.Name.Should().Be(name);
        stage.DisplayName.ShortName.Should().Be(shortName);
        stage.MatchFormat.Should().Be(matchFormat);
        stage.Rules.Should().Be(rules);
        stage.StandingRules.Should().Be(standingRules);
        stage.Labels.Should().BeSameAs(labels);
        stage.IsConsolation.Should().Be(isConsolation);
        stage.Type.Should().Be(StageType.Groups);
    }

    [Fact]
    public void Create_WithNullAncestorStageId_ShouldCreateGroupStage()
    {
        // Arrange
        const string name = "Group Stage";

        // Act
        var stage = GroupStage.Create(null, name);

        // Assert
        stage.Should().NotBeNull();
        stage.AncestorStageId.Should().BeNull();
        stage.DisplayName.Name.Should().Be(name);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => GroupStage.Create(ancestorStageId, string.Empty));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => GroupStage.Create(ancestorStageId, null!));
    }

    [Fact]
    public void AddGroup_WithValidTeamsAndName_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()), new ConcreteTeamReference(TeamId.New()) };
        const string groupName = "Group A";
        const string shortName = "A";

        // Act
        var result = stage.AddGroup(teams, groupName, shortName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DisplayName.Name.Should().Be(groupName);
        result.Value.DisplayName.ShortName.Should().Be(shortName);
        stage.Groups.Should().Contain(result.Value);
        stage.Groups.Should().HaveCount(1);
    }

    [Fact]
    public void AddGroup_WithoutShortName_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        const string groupName = "Group B";

        // Act
        var result = stage.AddGroup(teams, groupName, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Name.Should().Be(groupName);
        result.Value.DisplayName.ShortName.Should().Be("GB");
    }

    [Fact]
    public void AddGroup_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams1 = new[] { new ConcreteTeamReference(TeamId.New()) };
        var teams2 = new[] { new ConcreteTeamReference(TeamId.New()) };
        const string groupName = "Group A";

        stage.AddGroup(teams1, groupName, "A");

        // Act
        var result = stage.AddGroup(teams2, groupName, "A2");

        // Assert
        result.IsFailure.Should().BeTrue();
        stage.Groups.Should().HaveCount(1);
    }

    [Fact]
    public void AddGroup_WithMultipleGroups_ShouldAddAll()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams1 = new[] { new ConcreteTeamReference(TeamId.New()) };
        var teams2 = new[] { new ConcreteTeamReference(TeamId.New()) };
        var teams3 = new[] { new ConcreteTeamReference(TeamId.New()) };

        // Act
        var result1 = stage.AddGroup(teams1, "Group A", "A");
        var result2 = stage.AddGroup(teams2, "Group B", "B");
        var result3 = stage.AddGroup(teams3, "Group C", "C");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        stage.Groups.Should().HaveCount(3);
        stage.Groups.Select(static g => g.DisplayName.Name).Should().Contain(["Group A", "Group B", "Group C"]);
    }

    [Fact]
    public void RemoveGroup_WithExistingGroup_ShouldRemoveSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var addResult = stage.AddGroup(teams, "Group A", "A");
        var group = addResult.Value;

        // Act
        var result = stage.RemoveGroup(group);

        // Assert
        result.Should().BeTrue();
        stage.Groups.Should().BeEmpty();
    }

    [Fact]
    public void RemoveGroup_WithNonExistingGroup_ShouldReturnFalse()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var otherStage = GroupStage.Create(StageId.New(), "Other Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = otherStage.AddGroup(teams, "Group A", "A").Value;

        // Act
        var result = stage.RemoveGroup(group);

        // Assert
        result.Should().BeFalse();
        stage.Groups.Should().BeEmpty();
    }

    [Fact]
    public void ClearGroups_ShouldRemoveAllGroups()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams1 = new[] { new ConcreteTeamReference(TeamId.New()) };
        var teams2 = new[] { new ConcreteTeamReference(TeamId.New()) };
        stage.AddGroup(teams1, "Group A", "A");
        stage.AddGroup(teams2, "Group B", "B");

        // Act
        stage.ClearGroups();

        // Assert
        stage.Groups.Should().BeEmpty();
    }

    [Fact]
    public void AddMatchday_WithNewMatchdayId_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var matchdayId = MatchdayId.New();

        // Act
        var result = stage.AddMatchday(matchdayId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(matchdayId);
        stage.Matchdays.Should().Contain(matchdayId);
        stage.Matchdays.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveMatchday_WithExistingMatchdayId_ShouldRemoveSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var matchdayId = MatchdayId.New();
        stage.AddMatchday(matchdayId);

        // Act
        var result = stage.RemoveMatchday(matchdayId);

        // Assert
        result.Should().BeTrue();
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void ClearMatchdays_ShouldRemoveAllMatchdays()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var matchdayId1 = MatchdayId.New();
        var matchdayId2 = MatchdayId.New();
        stage.AddMatchday(matchdayId1);
        stage.AddMatchday(matchdayId2);

        // Act
        stage.ClearMatchdays();

        // Assert
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void AddPenalty_WithNewTeamId_ShouldAddPenaltyPoints()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId = TeamId.New();
        const int penaltyPoints = 5;

        // Act
        stage.AddPenalty(teamId, penaltyPoints);

        // Assert
        stage.PenaltyPoints.Should().ContainKey(teamId);
        stage.PenaltyPoints[teamId].Should().Be(penaltyPoints);
    }

    [Fact]
    public void AddPenalty_WithExistingTeamId_ShouldUpdatePenaltyPoints()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId = TeamId.New();
        const int initialPenalty = 3;
        const int newPenalty = 8;

        // Act
        stage.AddPenalty(teamId, initialPenalty);
        stage.AddPenalty(teamId, newPenalty);

        // Assert
        stage.PenaltyPoints.Should().ContainKey(teamId);
        stage.PenaltyPoints[teamId].Should().Be(newPenalty);
        stage.PenaltyPoints.Should().HaveCount(1);
    }

    [Fact]
    public void RemovePenalty_WithExistingTeamId_ShouldRemovePenalty()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId = TeamId.New();
        stage.AddPenalty(teamId, 5);

        // Act
        var result = stage.RemovePenalty(teamId);

        // Assert
        result.Should().BeTrue();
        stage.PenaltyPoints.Should().NotContainKey(teamId);
        stage.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void ClearPenaltyPoints_ShouldRemoveAllPenalties()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId1 = TeamId.New();
        var teamId2 = TeamId.New();
        stage.AddPenalty(teamId1, 3);
        stage.AddPenalty(teamId2, 5);

        // Act
        stage.ClearPenaltyPoints();

        // Assert
        stage.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void AddTeam_WithNewTeam_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());

        // Act
        var result = stage.AddTeam(teamRef);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(teamRef);
        stage.Teams.Should().Contain(teamRef);
        stage.Teams.Should().HaveCount(1);
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage", "GS");

        // Act
        var result = stage.ToString();

        // Assert
        result.Should().Be("Group Stage");
    }

    [Fact]
    public void StandingRules_ShouldBeSettable()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var newRules = StandingRuleSet.Default;

        // Act
        stage.StandingRules = newRules;

        // Assert
        stage.StandingRules.Should().Be(newRules);
    }

    [Fact]
    public void Labels_ShouldBeSettable()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var newLabels = new StandingLabels();

        // Act
        stage.Labels = newLabels;

        // Assert
        stage.Labels.Should().BeSameAs(newLabels);
    }

    [Fact]
    public void Groups_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teams = new[] { new ConcreteTeamReference(TeamId.New()) };
        var group = stage.AddGroup(teams, "Group A", "A").Value;

        // Act
        var groups = stage.Groups;

        // Assert
        groups.Should().BeAssignableTo<IReadOnlyCollection<Group>>();
        groups.Should().HaveCount(1);
        groups.Should().Contain(group);
    }

    [Fact]
    public void Matchdays_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var matchdayId = MatchdayId.New();
        stage.AddMatchday(matchdayId);

        // Act
        var matchdays = stage.Matchdays;

        // Assert
        matchdays.Should().BeAssignableTo<IReadOnlyCollection<MatchdayId>>();
        matchdays.Should().HaveCount(1);
        matchdays.Should().Contain(matchdayId);
    }

    [Fact]
    public void PenaltyPoints_ShouldBeReadOnlyDictionary()
    {
        // Arrange
        var stage = GroupStage.Create(StageId.New(), "Group Stage");
        var teamId = TeamId.New();
        stage.AddPenalty(teamId, 5);

        // Act
        var penaltyPoints = stage.PenaltyPoints;

        // Assert
        penaltyPoints.Should().BeAssignableTo<IReadOnlyDictionary<TeamId, int>>();
        penaltyPoints.Should().HaveCount(1);
        penaltyPoints.Should().ContainKey(teamId);
        penaltyPoints[teamId].Should().Be(5);
    }

    [Fact]
    public void UniqueIds_ShouldBeGeneratedForDifferentInstances()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act
        var stage1 = GroupStage.Create(ancestorStageId, "Stage 1");
        var stage2 = GroupStage.Create(ancestorStageId, "Stage 2");

        // Assert
        stage1.Id.Should().NotBe(stage2.Id);
        stage1.Id.Value.Should().NotBe(stage2.Id.Value);
    }
}
