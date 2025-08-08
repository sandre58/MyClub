// -----------------------------------------------------------------------
// <copyright file="TournamentTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.StageAggregate;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate;

public class TournamentTests
{
    private static Tournament CreateTestTournament(string name = "Test Tournament", string? shortName = null)
    {
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);
        return Tournament.Create(name, shortName, matchFormat, matchRules);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateTournament()
    {
        // Arrange
        const string name = "UEFA Euro";
        const string shortName = "Euro";
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);

        // Act
        var tournament = Tournament.Create(name, shortName, matchFormat, matchRules);

        // Assert
        tournament.Should().NotBeNull();
        tournament.Id.Should().NotBeNull();
        tournament.DisplayName.Name.Should().Be(name);
        tournament.DisplayName.ShortName.Should().Be(shortName);
        tournament.MatchFormat.Should().Be(matchFormat);
        tournament.MatchRules.Should().Be(matchRules);
        tournament.Stages.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithoutShortName_ShouldCreateTournament()
    {
        // Arrange
        const string name = "World Championship";
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);

        // Act
        var tournament = Tournament.Create(name, null, matchFormat, matchRules);

        // Assert
        tournament.DisplayName.Name.Should().Be(name);
        tournament.DisplayName.ShortName.Should().Be("WC");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrow(string? invalidName)
    {
        // Arrange
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);

        // Act & Assert
        var act = () => Tournament.Create(invalidName!, null, matchFormat, matchRules);
        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void Stages_ShouldBeEmptyOnCreation()
    {
        // Arrange & Act
        var tournament = CreateTestTournament();

        // Assert
        tournament.Stages.Should().BeEmpty();
    }

    [Fact]
    public void AddStage_ShouldAddStageToCollection()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId = StageId.New();

        // Act
        tournament.AddStage(stageId);

        // Assert
        tournament.Stages.Should().ContainSingle();
        tournament.Stages.Should().Contain(stageId);
    }

    [Fact]
    public void AddStage_WithSameStageTwice_ShouldOnlyAddOnce()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId = StageId.New();

        // Act
        tournament.AddStage(stageId);
        tournament.AddStage(stageId);

        // Assert
        tournament.Stages.Should().ContainSingle();
        tournament.Stages.Should().Contain(stageId);
    }

    [Fact]
    public void AddStage_WithMultipleStages_ShouldAddAllStages()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId1 = StageId.New();
        var stageId2 = StageId.New();
        var stageId3 = StageId.New();

        // Act
        tournament.AddStage(stageId1);
        tournament.AddStage(stageId2);
        tournament.AddStage(stageId3);

        // Assert
        tournament.Stages.Should().HaveCount(3);
        tournament.Stages.Should().Contain(stageId1);
        tournament.Stages.Should().Contain(stageId2);
        tournament.Stages.Should().Contain(stageId3);
    }

    [Fact]
    public void RemoveStage_WithExistingStage_ShouldRemoveStage()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId = StageId.New();
        tournament.AddStage(stageId);

        // Act
        tournament.RemoveStage(stageId);

        // Assert
        tournament.Stages.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStage_WithNonExistingStage_ShouldNotThrow()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId = StageId.New();

        // Act & Assert
        var act = () => tournament.RemoveStage(stageId);
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveStage_WithMultipleStages_ShouldRemoveOnlySpecifiedStage()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId1 = StageId.New();
        var stageId2 = StageId.New();
        var stageId3 = StageId.New();
        tournament.AddStage(stageId1);
        tournament.AddStage(stageId2);
        tournament.AddStage(stageId3);

        // Act
        tournament.RemoveStage(stageId2);

        // Assert
        tournament.Stages.Should().HaveCount(2);
        tournament.Stages.Should().Contain(stageId1);
        tournament.Stages.Should().NotContain(stageId2);
        tournament.Stages.Should().Contain(stageId3);
    }

    [Fact]
    public void Clear_WithStages_ShouldRemoveAllStages()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var stageId1 = StageId.New();
        var stageId2 = StageId.New();
        var stageId3 = StageId.New();
        tournament.AddStage(stageId1);
        tournament.AddStage(stageId2);
        tournament.AddStage(stageId3);

        // Act
        tournament.Clear();

        // Assert
        tournament.Stages.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WithEmptyStages_ShouldNotThrow()
    {
        // Arrange
        var tournament = CreateTestTournament();

        // Act & Assert
        var act = tournament.Clear;
        act.Should().NotThrow();
        tournament.Stages.Should().BeEmpty();
    }

    [Fact]
    public void Stages_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var tournament = CreateTestTournament();

        // Act
        var stages = tournament.Stages;

        // Assert
        stages.Should().BeAssignableTo<IReadOnlyCollection<StageId>>();
    }

    [Fact]
    public void DisplayName_ShouldBeInitializedCorrectly()
    {
        // Arrange
        const string name = "Champions League";
        const string shortName = "UCL";
        var tournament = CreateTestTournament(name, shortName);

        // Act & Assert
        tournament.DisplayName.Should().NotBeNull();
        tournament.DisplayName.Name.Should().Be(name);
        tournament.DisplayName.ShortName.Should().Be(shortName);
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange & Act
        var tournament1 = CreateTestTournament("Tournament 1");
        var tournament2 = CreateTestTournament("Tournament 2");

        // Assert
        tournament1.Id.Should().NotBe(tournament2.Id);
    }

    [Fact]
    public void MatchFormat_ShouldBeSettable()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var newMatchFormat = new MatchFormat(PeriodFormat.Default);

        // Act
        tournament.MatchFormat = newMatchFormat;

        // Assert
        tournament.MatchFormat.Should().Be(newMatchFormat);
    }

    [Fact]
    public void MatchRules_ShouldBeSettable()
    {
        // Arrange
        var tournament = CreateTestTournament();
        var newMatchRules = new MatchRules([]);

        // Act
        tournament.MatchRules = newMatchRules;

        // Assert
        tournament.MatchRules.Should().Be(newMatchRules);
    }
}
