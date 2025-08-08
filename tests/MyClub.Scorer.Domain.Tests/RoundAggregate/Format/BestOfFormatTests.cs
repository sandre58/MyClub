// -----------------------------------------------------------------------
// <copyright file="BestOfFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyNet.Utilities;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class BestOfFormatTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        const int maxGames = 7;
        var invertTeamsByStage = new[] { false, true, false, true, false, true, false };
        var regulationTime = PeriodFormat.Default;

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime);

        // Assert
        format.MaxGames.Should().Be(maxGames);
        format.InvertTeamsByStage.Should().BeEquivalentTo(invertTeamsByStage);
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().BeNull();
        format.NumberOfPenaltyShootouts.Should().BeNull();
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        const int maxGames = 5;
        var invertTeamsByStage = new[] { false, true, false, true, false };
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime, extraTime, numberOfPenaltyShootouts);

        // Assert
        format.MaxGames.Should().Be(maxGames);
        format.InvertTeamsByStage.Should().BeEquivalentTo(invertTeamsByStage);
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().Be(extraTime);
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void Type_ShouldReturnBestOf()
    {
        // Arrange
        var format = new BestOfFormat(3, [false, true, false], PeriodFormat.Default);

        // Act & Assert
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void AllowDraw_ShouldReturnFalse()
    {
        // Arrange
        var format = new BestOfFormat(3, [false, true, false], PeriodFormat.Default);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void AllowDraw_WithExtraTime_ShouldStillReturnFalse()
    {
        // Arrange
        var format = new BestOfFormat(3, [false, true, false], PeriodFormat.Default, PeriodFormat.ExtraTime);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void AllowDraw_WithPenaltyShootouts_ShouldStillReturnFalse()
    {
        // Arrange
        var format = new BestOfFormat(3, [false, true, false], PeriodFormat.Default, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        const int maxGames = 5;
        var invertTeamsByStage = new[] { false, true, false, true, false };
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;

        var format1 = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime, extraTime, numberOfPenaltyShootouts);
        var format2 = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime, extraTime, numberOfPenaltyShootouts);

        // Act & Assert
        format1.Should().Be(format2);
        format1.GetHashCode().Should().Be(format2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentMaxGames_ShouldNotBeEqual()
    {
        // Arrange
        var invertTeamsByStage = new[] { false, true, false };
        var regulationTime = PeriodFormat.Default;

        var format1 = new BestOfFormat(3, invertTeamsByStage, regulationTime);
        var format2 = new BestOfFormat(5, invertTeamsByStage, regulationTime);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentInvertTeamsByStage_ShouldNotBeEqual()
    {
        // Arrange
        const int maxGames = 3;
        var invertTeamsByStage1 = new[] { false, true, false };
        var invertTeamsByStage2 = new[] { true, false, true };
        var regulationTime = PeriodFormat.Default;

        var format1 = new BestOfFormat(maxGames, invertTeamsByStage1, regulationTime);
        var format2 = new BestOfFormat(maxGames, invertTeamsByStage2, regulationTime);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentRegulationTime_ShouldNotBeEqual()
    {
        // Arrange
        const int maxGames = 3;
        var invertTeamsByStage = new[] { false, true, false };
        var regulationTime1 = PeriodFormat.Default;
        var regulationTime2 = new PeriodFormat(2, 40.Minutes(), 10.Minutes());

        var format1 = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime1);
        var format2 = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var format = new BestOfFormat(3, [false, true, false], PeriodFormat.Default);

        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("BestOfFormat");
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        const int maxGames = 5;
        var invertTeamsByStage = new[] { false, true, false, true, false };
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;
        var format = new BestOfFormat(maxGames, invertTeamsByStage, regulationTime, extraTime, numberOfPenaltyShootouts);

        // Act
        var (actualMaxGames, actualInvertTeamsByStage, actualRegulationTime, actualExtraTime, actualNumberOfPenaltyShootouts) = format;

        // Assert
        actualMaxGames.Should().Be(maxGames);
        actualInvertTeamsByStage.Should().BeEquivalentTo(invertTeamsByStage);
        actualRegulationTime.Should().Be(regulationTime);
        actualExtraTime.Should().Be(extraTime);
        actualNumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var originalFormat = new BestOfFormat(3, [false, true, false], PeriodFormat.Default);
        const int newMaxGames = 5;
        var newInvertTeamsByStage = new[] { true, false, true, false, true };

        // Act
        var modifiedFormat = originalFormat with { MaxGames = newMaxGames, InvertTeamsByStage = newInvertTeamsByStage };

        // Assert
        modifiedFormat.MaxGames.Should().Be(newMaxGames);
        modifiedFormat.InvertTeamsByStage.Should().BeEquivalentTo(newInvertTeamsByStage);
        modifiedFormat.RegulationTime.Should().Be(originalFormat.RegulationTime);
        modifiedFormat.Type.Should().Be(RoundFormatType.BestOf);
        modifiedFormat.AllowDraw().Should().BeFalse();

        // The Original should remain unchanged
        originalFormat.MaxGames.Should().Be(3);
        originalFormat.InvertTeamsByStage.Should().BeEquivalentTo([false, true, false]);
    }

    [Fact]
    public void CreateBestOfFormat_WithBestOfThree_ShouldWork()
    {
        // Arrange
        const int maxGames = 3;
        var invertTeamsByStage = new[] { false, true, false };

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.MaxGames.Should().Be(3);
        format.InvertTeamsByStage.Should().HaveCount(3);
        format.AllowDraw().Should().BeFalse();
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void CreateBestOfFormat_WithBestOfFive_ShouldWork()
    {
        // Arrange
        const int maxGames = 5;
        var invertTeamsByStage = new[] { false, true, false, true, false };

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.MaxGames.Should().Be(5);
        format.InvertTeamsByStage.Should().HaveCount(5);
        format.AllowDraw().Should().BeFalse();
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void CreateBestOfFormat_WithBestOfSeven_ShouldWork()
    {
        // Arrange
        const int maxGames = 7;
        var invertTeamsByStage = new[] { false, true, false, true, false, true, false };

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.MaxGames.Should().Be(7);
        format.InvertTeamsByStage.Should().HaveCount(7);
        format.AllowDraw().Should().BeFalse();
        format.Type.Should().Be(RoundFormatType.BestOf);
    }

    [Fact]
    public void CreateBestOfFormat_WithEmptyInvertTeamsByStage_ShouldWork()
    {
        // Arrange
        const int maxGames = 0;
        var invertTeamsByStage = Array.Empty<bool>();

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.MaxGames.Should().Be(0);
        format.InvertTeamsByStage.Should().BeEmpty();
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void CreateBestOfFormat_WithAllTrueInvertTeamsByStage_ShouldWork()
    {
        // Arrange
        const int maxGames = 3;
        var invertTeamsByStage = new[] { true, true, true };

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.InvertTeamsByStage.Should().AllSatisfy(static x => x.Should().BeTrue());
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void CreateBestOfFormat_WithAllFalseInvertTeamsByStage_ShouldWork()
    {
        // Arrange
        const int maxGames = 3;
        var invertTeamsByStage = new[] { false, false, false };

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.InvertTeamsByStage.Should().AllSatisfy(static x => x.Should().BeFalse());
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void CreateBestOfFormat_WithMismatchedArraySize_ShouldStillWork()
    {
        // Arrange
        const int maxGames = 5;
        var invertTeamsByStage = new[] { false, true, false }; // Only 3 elements for 5 games

        // Act
        var format = new BestOfFormat(maxGames, invertTeamsByStage, PeriodFormat.Default);

        // Assert
        format.MaxGames.Should().Be(5);
        format.InvertTeamsByStage.Should().HaveCount(3);
        format.AllowDraw().Should().BeFalse();
    }
}
