// -----------------------------------------------------------------------
// <copyright file="ChampionshipStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class ChampionshipStageTests
{
    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateChampionshipStage()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "Championship Stage";

        // Act
        var stage = ChampionshipStage.Create(ancestorStageId, name);

        // Assert
        stage.Should().NotBeNull();
        stage.Id.Should().NotBeNull();
        stage.AncestorStageId.Should().Be(ancestorStageId);
        stage.DisplayName.Name.Should().Be(name);
        stage.DisplayName.ShortName.Should().Be("CS");
        stage.Type.Should().Be(StageType.Championship);
        stage.IsConsolation.Should().BeFalse();
        stage.MatchFormat.Should().Be(MatchFormat.Default);
        stage.Rules.Should().Be(MatchRules.Default);
        stage.StandingRules.Should().Be(StandingRuleSet.Default);
        stage.Labels.Should().BeEmpty();
        stage.Matchdays.Should().BeEmpty();
        stage.Teams.Should().BeEmpty();
        stage.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateChampionshipStageWithSpecifiedValues()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "League";
        const string shortName = "L1";
        var matchFormat = MatchFormat.Default;
        var rules = MatchRules.Default;
        var standingRules = StandingRuleSet.Default;
        var labels = new StandingLabels();
        const bool isConsolation = true;

        // Act
        var stage = ChampionshipStage.Create(ancestorStageId, name, shortName, matchFormat, rules, standingRules, labels, isConsolation);

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
        stage.Type.Should().Be(StageType.Championship);
    }

    [Fact]
    public void Create_WithNullAncestorStageId_ShouldCreateChampionshipStage()
    {
        // Arrange
        const string name = "Championship Stage";

        // Act
        var stage = ChampionshipStage.Create(null, name);

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
        Assert.Throws<NullOrEmptyException>(() => ChampionshipStage.Create(ancestorStageId, string.Empty));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => ChampionshipStage.Create(ancestorStageId, null!));
    }

    [Fact]
    public void AddMatchday_WithNewMatchdayId_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
    public void AddMatchday_WithMultipleMatchdays_ShouldAddAll()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var matchdayId1 = MatchdayId.New();
        var matchdayId2 = MatchdayId.New();
        var matchdayId3 = MatchdayId.New();

        // Act
        var result1 = stage.AddMatchday(matchdayId1);
        var result2 = stage.AddMatchday(matchdayId2);
        var result3 = stage.AddMatchday(matchdayId3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        stage.Matchdays.Should().HaveCount(3);
        stage.Matchdays.Should().Contain([matchdayId1, matchdayId2, matchdayId3]);
    }

    [Fact]
    public void RemoveMatchday_WithExistingMatchdayId_ShouldRemoveSuccessfully()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var matchdayId = MatchdayId.New();
        stage.AddMatchday(matchdayId);

        // Act
        var result = stage.RemoveMatchday(matchdayId);

        // Assert
        result.Should().BeTrue();
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMatchday_WithNonExistingMatchdayId_ShouldReturnFalse()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var matchdayId = MatchdayId.New();

        // Act
        var result = stage.RemoveMatchday(matchdayId);

        // Assert
        result.Should().BeFalse();
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllMatchdays()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var matchdayId1 = MatchdayId.New();
        var matchdayId2 = MatchdayId.New();
        stage.AddMatchday(matchdayId1);
        stage.AddMatchday(matchdayId2);

        // Act
        stage.Clear();

        // Assert
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void AddPenalty_WithNewTeamId_ShouldAddPenaltyPoints()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
    public void RemovePenalty_WithNonExistingTeamId_ShouldReturnFalse()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var teamId = TeamId.New();

        // Act
        var result = stage.RemovePenalty(teamId);

        // Assert
        result.Should().BeFalse();
        stage.PenaltyPoints.Should().BeEmpty();
    }

    [Fact]
    public void ClearPenaltyPoints_ShouldRemoveAllPenalties()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
    public void AddTeam_WithExistingTeam_ShouldReturnFailure()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());
        stage.AddTeam(teamRef);

        // Act
        var result = stage.AddTeam(teamRef);

        // Assert
        result.IsFailure.Should().BeTrue();
        stage.Teams.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTeam_WithExistingTeam_ShouldRemoveSuccessfully()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());
        stage.AddTeam(teamRef);

        // Act
        var result = stage.RemoveTeam(teamRef);

        // Assert
        result.Should().BeTrue();
        stage.Teams.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage", "CS");

        // Act
        var result = stage.ToString();

        // Assert
        result.Should().Be("Championship Stage");
    }

    [Fact]
    public void StandingRules_ShouldBeSettable()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
        var newLabels = new StandingLabels();

        // Act
        stage.Labels = newLabels;

        // Assert
        stage.Labels.Should().BeSameAs(newLabels);
    }

    [Fact]
    public void Matchdays_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage = ChampionshipStage.Create(StageId.New(), "Championship Stage");
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
        var stage1 = ChampionshipStage.Create(ancestorStageId, "Stage 1");
        var stage2 = ChampionshipStage.Create(ancestorStageId, "Stage 2");

        // Assert
        stage1.Id.Should().NotBe(stage2.Id);
        stage1.Id.Value.Should().NotBe(stage2.Id.Value);
    }
}
