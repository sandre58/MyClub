// -----------------------------------------------------------------------
// <copyright file="RoundStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FluentAssertions;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Domain.Matches;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate;

public class RoundStageTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateRoundStage()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15, 14, 30, 0);
        const string name = "Semi-Final";
        const string shortName = "SF";

        // Act
        var roundStage = RoundStage.Create(date, name, shortName);

        // Assert
        roundStage.Should().NotBeNull();
        roundStage.Id.Should().NotBeNull();
        roundStage.OriginDate.Should().Be(date);
        roundStage.Date.Should().Be(date);
        roundStage.DisplayName.Name.Should().Be(name);
        roundStage.DisplayName.ShortName.Should().Be(shortName);
        roundStage.IsPostponed.Should().BeFalse();
        roundStage.Matches.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNameOnly_ShouldCreateRoundStageWithNullShortName()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15);
        const string name = "Final";

        // Act
        var roundStage = RoundStage.Create(date, name);

        // Assert
        roundStage.Should().NotBeNull();
        roundStage.DisplayName.Name.Should().Be(name);
        roundStage.DisplayName.ShortName.Should().Be("F");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15);

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => RoundStage.Create(date, string.Empty));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowNullOrEmptyException()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15);

        // Act & Assert
        Assert.Throws<NullOrEmptyException>(() => RoundStage.Create(date, null!));
    }

    [Fact]
    public void Postpone_WithoutDate_ShouldMarkAsPostponedWithoutChangingDate()
    {
        // Arrange
        var originalDate = new DateTime(2024, 3, 15);
        var roundStage = RoundStage.Create(originalDate, "Semi-Final");

        // Act
        roundStage.Postpone();

        // Assert
        roundStage.IsPostponed.Should().BeTrue();
        roundStage.OriginDate.Should().Be(originalDate);
        roundStage.Date.Should().Be(originalDate);
    }

    [Fact]
    public void Postpone_WithDate_ShouldMarkAsPostponedAndChangeDate()
    {
        // Arrange
        var originalDate = new DateTime(2024, 3, 15);
        var postponedDate = new DateTime(2024, 3, 22);
        var roundStage = RoundStage.Create(originalDate, "Semi-Final");

        // Act
        roundStage.Postpone(postponedDate);

        // Assert
        roundStage.IsPostponed.Should().BeTrue();
        roundStage.OriginDate.Should().Be(originalDate);
        roundStage.Date.Should().Be(postponedDate);
    }

    [Fact]
    public void Schedule_AfterPostpone_ShouldResetPostponedState()
    {
        // Arrange
        var originalDate = new DateTime(2024, 3, 15);
        var postponedDate = new DateTime(2024, 3, 22);
        var newDate = new DateTime(2024, 3, 29);
        var roundStage = RoundStage.Create(originalDate, "Semi-Final");
        roundStage.Postpone(postponedDate);

        // Act
        roundStage.Schedule(newDate);

        // Assert
        roundStage.IsPostponed.Should().BeFalse();
        roundStage.OriginDate.Should().Be(newDate);
        roundStage.Date.Should().Be(newDate);
    }

    [Fact]
    public void AddMatch_WithNewMatchId_ShouldAddSuccessfully()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId = MatchId.New();

        // Act
        var result = roundStage.AddMatch(matchId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(matchId);
        roundStage.Matches.Should().Contain(matchId);
        roundStage.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void AddMatch_WithExistingMatchId_ShouldReturnFailure()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId = MatchId.New();
        roundStage.AddMatch(matchId);

        // Act
        var result = roundStage.AddMatch(matchId);

        // Assert
        result.IsFailure.Should().BeTrue();
        roundStage.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void AddMatch_WithMultipleMatches_ShouldAddAll()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId1 = MatchId.New();
        var matchId2 = MatchId.New();
        var matchId3 = MatchId.New();

        // Act
        var result1 = roundStage.AddMatch(matchId1);
        var result2 = roundStage.AddMatch(matchId2);
        var result3 = roundStage.AddMatch(matchId3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        roundStage.Matches.Should().HaveCount(3);
        roundStage.Matches.Should().Contain([matchId1, matchId2, matchId3]);
    }

    [Fact]
    public void RemoveMatch_WithExistingMatchId_ShouldRemoveSuccessfully()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId = MatchId.New();
        roundStage.AddMatch(matchId);

        // Act
        var result = roundStage.RemoveMatch(matchId);

        // Assert
        result.Should().BeTrue();
        roundStage.Matches.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMatch_WithNonExistingMatchId_ShouldReturnFalse()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId = MatchId.New();

        // Act
        var result = roundStage.RemoveMatch(matchId);

        // Assert
        result.Should().BeFalse();
        roundStage.Matches.Should().BeEmpty();
    }

    [Fact]
    public void CompareTo_WithEarlierDate_ShouldReturnNegative()
    {
        // Arrange
        var earlierStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var laterStage = RoundStage.Create(new(2024, 3, 22), "Final");

        // Act
        var result = earlierStage.CompareTo(laterStage);

        // Assert
        result.Should().BeNegative();
    }

    [Fact]
    public void CompareTo_WithLaterDate_ShouldReturnPositive()
    {
        // Arrange
        var earlierStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var laterStage = RoundStage.Create(new(2024, 3, 22), "Final");

        // Act
        var result = laterStage.CompareTo(earlierStage);

        // Assert
        result.Should().BePositive();
    }

    [Fact]
    public void CompareTo_WithSameDate_ShouldReturnZero()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15);
        var stage1 = RoundStage.Create(date, "Match 1");
        var stage2 = RoundStage.Create(date, "Match 2");

        // Act
        var result = stage1.CompareTo(stage2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");

        // Act
        var result = roundStage.CompareTo(null);

        // Assert
        result.Should().BePositive();
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final", "SF");

        // Act
        var result = roundStage.ToString();

        // Assert
        result.Should().Be("Semi-Final");
    }

    [Fact]
    public void DisplayName_ShouldExposeNameAndShortName()
    {
        // Arrange
        const string name = "Quarter-Final";
        const string shortName = "QF";
        var roundStage = RoundStage.Create(new(2024, 3, 15), name, shortName);

        // Act & Assert
        roundStage.DisplayName.Name.Should().Be(name);
        roundStage.DisplayName.ShortName.Should().Be(shortName);
    }

    [Fact]
    public void Matches_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var roundStage = RoundStage.Create(new(2024, 3, 15), "Semi-Final");
        var matchId = MatchId.New();
        roundStage.AddMatch(matchId);

        // Act
        var matches = roundStage.Matches;

        // Assert
        matches.Should().BeAssignableTo<IReadOnlyCollection<MatchId>>();
        matches.Should().HaveCount(1);
        matches.Should().Contain(matchId);
    }

    [Fact]
    public void Date_WhenNotPostponed_ShouldReturnOriginDate()
    {
        // Arrange
        var originalDate = new DateTime(2024, 3, 15);
        var roundStage = RoundStage.Create(originalDate, "Semi-Final");

        // Act & Assert
        roundStage.Date.Should().Be(originalDate);
        roundStage.OriginDate.Should().Be(originalDate);
    }

    [Fact]
    public void Date_WhenPostponed_ShouldReturnPostponedDate()
    {
        // Arrange
        var originalDate = new DateTime(2024, 3, 15);
        var postponedDate = new DateTime(2024, 3, 22);
        var roundStage = RoundStage.Create(originalDate, "Semi-Final");

        // Act
        roundStage.Postpone(postponedDate);

        // Assert
        roundStage.Date.Should().Be(postponedDate);
        roundStage.OriginDate.Should().Be(originalDate);
    }

    [Fact]
    public void UniqueIds_ShouldBeGeneratedForDifferentInstances()
    {
        // Arrange & Act
        var stage1 = RoundStage.Create(new(2024, 3, 15), "Stage 1");
        var stage2 = RoundStage.Create(new(2024, 3, 15), "Stage 2");

        // Assert
        stage1.Id.Should().NotBe(stage2.Id);
        stage1.Id.Value.Should().NotBe(stage2.Id.Value);
    }
}
