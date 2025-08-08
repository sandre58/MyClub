// -----------------------------------------------------------------------
// <copyright file="CupTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate;

public class CupTests
{
    private static Cup CreateTestCup(string name = "Test Cup", string? shortName = null)
    {
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);
        return Cup.Create(name, shortName, matchFormat, matchRules);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateCup()
    {
        // Arrange
        const string name = "FA Cup";
        const string shortName = "FA";
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);

        // Act
        var cup = Cup.Create(name, shortName, matchFormat, matchRules);

        // Assert
        cup.Should().NotBeNull();
        cup.Id.Should().NotBeNull();
        cup.DisplayName.Name.Should().Be(name);
        cup.DisplayName.ShortName.Should().Be(shortName);
        cup.MatchFormat.Should().Be(matchFormat);
        cup.MatchRules.Should().Be(matchRules);
        cup.Type.Should().Be(CompetitionType.Cup);
        cup.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithoutShortName_ShouldCreateCup()
    {
        // Arrange
        const string name = "Champions League";
        var matchFormat = new MatchFormat(PeriodFormat.Default);
        var matchRules = new MatchRules([]);

        // Act
        var cup = Cup.Create(name, null, matchFormat, matchRules);

        // Assert
        cup.DisplayName.Name.Should().Be(name);
        cup.DisplayName.ShortName.Should().Be("CL");
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
        var act = () => Cup.Create(invalidName!, null, matchFormat, matchRules);
        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void Type_ShouldReturnCup()
    {
        // Arrange & Act
        var cup = CreateTestCup();

        // Assert
        cup.Type.Should().Be(CompetitionType.Cup);
    }

    [Fact]
    public void Rounds_ShouldBeEmptyOnCreation()
    {
        // Arrange & Act
        var cup = CreateTestCup();

        // Assert
        cup.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void AddRound_ShouldAddRoundToCollection()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId = RoundId.New();

        // Act
        cup.AddRound(roundId);

        // Assert
        cup.Rounds.Should().ContainSingle();
        cup.Rounds.Should().Contain(roundId);
    }

    [Fact]
    public void AddRound_WithSameRoundTwice_ShouldOnlyAddOnce()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId = RoundId.New();

        // Act
        cup.AddRound(roundId);
        cup.AddRound(roundId);

        // Assert
        cup.Rounds.Should().ContainSingle();
        cup.Rounds.Should().Contain(roundId);
    }

    [Fact]
    public void AddRound_WithMultipleRounds_ShouldAddAllRounds()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId1 = RoundId.New();
        var roundId2 = RoundId.New();
        var roundId3 = RoundId.New();

        // Act
        cup.AddRound(roundId1);
        cup.AddRound(roundId2);
        cup.AddRound(roundId3);

        // Assert
        cup.Rounds.Should().HaveCount(3);
        cup.Rounds.Should().Contain(roundId1);
        cup.Rounds.Should().Contain(roundId2);
        cup.Rounds.Should().Contain(roundId3);
    }

    [Fact]
    public void RemoveRound_WithExistingRound_ShouldRemoveRound()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId = RoundId.New();
        cup.AddRound(roundId);

        // Act
        cup.RemoveRound(roundId);

        // Assert
        cup.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRound_WithNonExistingRound_ShouldNotThrow()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId = RoundId.New();

        // Act & Assert
        var act = () => cup.RemoveRound(roundId);
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveRound_WithMultipleRounds_ShouldRemoveOnlySpecifiedRound()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId1 = RoundId.New();
        var roundId2 = RoundId.New();
        var roundId3 = RoundId.New();
        cup.AddRound(roundId1);
        cup.AddRound(roundId2);
        cup.AddRound(roundId3);

        // Act
        cup.RemoveRound(roundId2);

        // Assert
        cup.Rounds.Should().HaveCount(2);
        cup.Rounds.Should().Contain(roundId1);
        cup.Rounds.Should().NotContain(roundId2);
        cup.Rounds.Should().Contain(roundId3);
    }

    [Fact]
    public void Clear_WithRounds_ShouldRemoveAllRounds()
    {
        // Arrange
        var cup = CreateTestCup();
        var roundId1 = RoundId.New();
        var roundId2 = RoundId.New();
        var roundId3 = RoundId.New();
        cup.AddRound(roundId1);
        cup.AddRound(roundId2);
        cup.AddRound(roundId3);

        // Act
        cup.Clear();

        // Assert
        cup.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WithEmptyRounds_ShouldNotThrow()
    {
        // Arrange
        var cup = CreateTestCup();

        // Act & Assert
        var act = cup.Clear;
        act.Should().NotThrow();
        cup.Rounds.Should().BeEmpty();
    }

    [Fact]
    public void Rounds_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var cup = CreateTestCup();

        // Act
        var rounds = cup.Rounds;

        // Assert
        rounds.Should().BeAssignableTo<IReadOnlyCollection<RoundId>>();
    }

    [Fact]
    public void DisplayName_ShouldBeInitializedCorrectly()
    {
        // Arrange
        const string name = "World Cup";
        const string shortName = "WC";
        var cup = CreateTestCup(name, shortName);

        // Act & Assert
        cup.DisplayName.Should().NotBeNull();
        cup.DisplayName.Name.Should().Be(name);
        cup.DisplayName.ShortName.Should().Be(shortName);
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Arrange & Act
        var cup1 = CreateTestCup("Cup 1");
        var cup2 = CreateTestCup("Cup 2");

        // Assert
        cup1.Id.Should().NotBe(cup2.Id);
    }

    [Fact]
    public void MatchFormat_ShouldBeSettable()
    {
        // Arrange
        var cup = CreateTestCup();
        var newMatchFormat = new MatchFormat(PeriodFormat.Default);

        // Act
        cup.MatchFormat = newMatchFormat;

        // Assert
        cup.MatchFormat.Should().Be(newMatchFormat);
    }

    [Fact]
    public void MatchRules_ShouldBeSettable()
    {
        // Arrange
        var cup = CreateTestCup();
        var newMatchRules = new MatchRules([]);

        // Act
        cup.MatchRules = newMatchRules;

        // Assert
        cup.MatchRules.Should().Be(newMatchRules);
    }
}
