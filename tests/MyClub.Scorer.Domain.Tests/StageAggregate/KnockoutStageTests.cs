// -----------------------------------------------------------------------
// <copyright file="KnockoutStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class KnockoutStageTests
{
    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateKnockoutStage()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "Knockout Stage";

        // Act
        var stage = KnockoutStage.Create(ancestorStageId, name);

        // Assert
        stage.Should().NotBeNull();
        stage.Id.Should().NotBeNull();
        stage.AncestorStageId.Should().Be(ancestorStageId);
        stage.DisplayName.Name.Should().Be(name);
        stage.DisplayName.ShortName.Should().Be("KS");
        stage.Type.Should().Be(StageType.Knockout);
        stage.IsConsolation.Should().BeFalse();
        stage.MatchFormat.Should().Be(MatchFormat.NoDraw);
        stage.Rules.Should().Be(MatchRules.Default);
        stage.Rounds.Should().BeEmpty();
        stage.Teams.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateKnockoutStageWithSpecifiedValues()
    {
        // Arrange
        var ancestorStageId = StageId.New();
        const string name = "Semi-Final";
        const string shortName = "SF";
        var matchFormat = MatchFormat.Default;
        var rules = MatchRules.Default;
        const bool isConsolation = true;

        // Act
        var stage = KnockoutStage.Create(ancestorStageId, name, shortName, matchFormat, rules, isConsolation);

        // Assert
        stage.Should().NotBeNull();
        stage.AncestorStageId.Should().Be(ancestorStageId);
        stage.DisplayName.Name.Should().Be(name);
        stage.DisplayName.ShortName.Should().Be(shortName);
        stage.MatchFormat.Should().Be(matchFormat);
        stage.Rules.Should().Be(rules);
        stage.IsConsolation.Should().Be(isConsolation);
        stage.Type.Should().Be(StageType.Knockout);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => KnockoutStage.Create(ancestorStageId, string.Empty));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => KnockoutStage.Create(ancestorStageId, null!));
    }

    [Fact]
    public void AddRound_WithNewRoundId_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId = RoundId.New();

        // Act
        var result = stage.AddRound(roundId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(roundId);
        stage.Rounds.Should().Contain(roundId);
        stage.Rounds.Should().HaveCount(1);
    }

    [Fact]
    public void AddRound_WithMultipleRounds_ShouldAddAll()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId1 = RoundId.New();
        var roundId2 = RoundId.New();
        var roundId3 = RoundId.New();

        // Act
        var result1 = stage.AddRound(roundId1);
        var result2 = stage.AddRound(roundId2);
        var result3 = stage.AddRound(roundId3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        stage.Rounds.Should().HaveCount(3);
        stage.Rounds.Should().Contain([roundId1, roundId2, roundId3]);
    }

    [Fact]
    public void RemoveRound_WithExistingRoundId_ShouldRemoveSuccessfully()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId = RoundId.New();
        stage.AddRound(roundId);

        // Act
        var result = stage.RemoveRound(roundId);

        // Assert
        result.Should().BeTrue();
        stage.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRound_WithNonExistingRoundId_ShouldReturnFalse()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId = RoundId.New();

        // Act
        var result = stage.RemoveRound(roundId);

        // Assert
        result.Should().BeFalse();
        stage.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllRounds()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId1 = RoundId.New();
        var roundId2 = RoundId.New();
        stage.AddRound(roundId1);
        stage.AddRound(roundId2);

        // Act
        stage.Clear();

        // Assert
        stage.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void AddTeam_WithNewTeam_ShouldAddSuccessfully()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
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
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
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
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());
        stage.AddTeam(teamRef);

        // Act
        var result = stage.RemoveTeam(teamRef);

        // Assert
        result.Should().BeTrue();
        stage.Teams.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTeam_WithNonExistingTeam_ShouldReturnFalse()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var teamRef = new ConcreteTeamReference(TeamId.New());

        // Act
        var result = stage.RemoveTeam(teamRef);

        // Assert
        result.Should().BeFalse();
        stage.Teams.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage", "KO");

        // Act
        var result = stage.ToString();

        // Assert
        result.Should().Be("Knockout Stage");
    }

    [Fact]
    public void MatchFormat_ShouldBeSettable()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var newFormat = MatchFormat.Default;

        // Act
        stage.MatchFormat = newFormat;

        // Assert
        stage.MatchFormat.Should().Be(newFormat);
    }

    [Fact]
    public void Rules_ShouldBeSettable()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var newRules = MatchRules.Default;

        // Act
        stage.Rules = newRules;

        // Assert
        stage.Rules.Should().Be(newRules);
    }

    [Fact]
    public void Rounds_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var stage = KnockoutStage.Create(StageId.New(), "Knockout Stage");
        var roundId = RoundId.New();
        stage.AddRound(roundId);

        // Act
        var rounds = stage.Rounds;

        // Assert
        rounds.Should().BeAssignableTo<IReadOnlyCollection<RoundId>>();
        rounds.Should().HaveCount(1);
        rounds.Should().Contain(roundId);
    }

    [Fact]
    public void UniqueIds_ShouldBeGeneratedForDifferentInstances()
    {
        // Arrange
        var ancestorStageId = StageId.New();

        // Act
        var stage1 = KnockoutStage.Create(ancestorStageId, "Stage 1");
        var stage2 = KnockoutStage.Create(ancestorStageId, "Stage 2");

        // Assert
        stage1.Id.Should().NotBe(stage2.Id);
        stage1.Id.Value.Should().NotBe(stage2.Id.Value);
    }
}
