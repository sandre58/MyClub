// -----------------------------------------------------------------------
// <copyright file="HomeAndAwayFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyNet.Utilities;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class HomeAndAwayFormatTests
{
    [Fact]
    public void Constructor_WithValidRegulationTime_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        // Act
        var format = new HomeAndAwayFormat(regulationTime);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().BeNull();
        format.UseAwayGoals.Should().BeFalse();
        format.NumberOfPenaltyShootouts.Should().BeNull();
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());
        const bool useAwayGoals = true;
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new HomeAndAwayFormat(regulationTime, extraTime, useAwayGoals, numberOfPenaltyShootouts);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().Be(extraTime);
        format.UseAwayGoals.Should().Be(useAwayGoals);
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }

    [Fact]
    public void Type_ShouldReturnHomeAndAway()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default);

        // Act & Assert
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }

    [Fact]
    public void AllowDraw_ShouldReturnTrue()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void AllowDraw_WithExtraTime_ShouldStillReturnTrue()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default, PeriodFormat.ExtraTime);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void AllowDraw_WithAwayGoals_ShouldStillReturnTrue()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default, UseAwayGoals: true);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void AllowDraw_WithPenaltyShootouts_ShouldStillReturnTrue()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const bool useAwayGoals = true;
        const int numberOfPenaltyShootouts = 5;

        var format1 = new HomeAndAwayFormat(regulationTime, extraTime, useAwayGoals, numberOfPenaltyShootouts);
        var format2 = new HomeAndAwayFormat(regulationTime, extraTime, useAwayGoals, numberOfPenaltyShootouts);

        // Act & Assert
        format1.Should().Be(format2);
        format1.GetHashCode().Should().Be(format2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentRegulationTime_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime1 = PeriodFormat.Default;
        var regulationTime2 = new PeriodFormat(2, 40.Minutes(), 10.Minutes());

        var format1 = new HomeAndAwayFormat(regulationTime1);
        var format2 = new HomeAndAwayFormat(regulationTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentExtraTime_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime1 = PeriodFormat.ExtraTime;
        var extraTime2 = new PeriodFormat(2, 10.Minutes(), 2.Minutes());

        var format1 = new HomeAndAwayFormat(regulationTime, extraTime1);
        var format2 = new HomeAndAwayFormat(regulationTime, extraTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentUseAwayGoals_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        var format1 = new HomeAndAwayFormat(regulationTime, UseAwayGoals: true);
        var format2 = new HomeAndAwayFormat(regulationTime, UseAwayGoals: false);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentNumberOfPenaltyShootouts_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        var format1 = new HomeAndAwayFormat(regulationTime, NumberOfPenaltyShootouts: 3);
        var format2 = new HomeAndAwayFormat(regulationTime, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var format = new HomeAndAwayFormat(PeriodFormat.Default);

        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("HomeAndAwayFormat");
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const bool useAwayGoals = true;
        const int numberOfPenaltyShootouts = 5;
        var format = new HomeAndAwayFormat(regulationTime, extraTime, useAwayGoals, numberOfPenaltyShootouts);

        // Act
        var (actualRegulationTime, actualExtraTime, actualUseAwayGoals, actualNumberOfPenaltyShootouts) = format;

        // Assert
        actualRegulationTime.Should().Be(regulationTime);
        actualExtraTime.Should().Be(extraTime);
        actualUseAwayGoals.Should().Be(useAwayGoals);
        actualNumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var originalFormat = new HomeAndAwayFormat(PeriodFormat.Default, UseAwayGoals: false);
        var newExtraTime = PeriodFormat.ExtraTime;

        // Act
        var modifiedFormat = originalFormat with { ExtraTime = newExtraTime, UseAwayGoals = true };

        // Assert
        modifiedFormat.RegulationTime.Should().Be(originalFormat.RegulationTime);
        modifiedFormat.ExtraTime.Should().Be(newExtraTime);
        modifiedFormat.UseAwayGoals.Should().BeTrue();
        modifiedFormat.NumberOfPenaltyShootouts.Should().Be(originalFormat.NumberOfPenaltyShootouts);
        modifiedFormat.Type.Should().Be(RoundFormatType.HomeAndAway);
        modifiedFormat.AllowDraw().Should().BeTrue();

        // The Original should remain unchanged
        originalFormat.ExtraTime.Should().BeNull();
        originalFormat.UseAwayGoals.Should().BeFalse();
    }

    [Fact]
    public void CreateHomeAndAwayFormat_WithAwayGoalsEnabled_ShouldWork()
    {
        // Act
        var format = new HomeAndAwayFormat(PeriodFormat.Default, UseAwayGoals: true);

        // Assert
        format.UseAwayGoals.Should().BeTrue();
        format.AllowDraw().Should().BeTrue();
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }

    [Fact]
    public void CreateHomeAndAwayFormat_WithAwayGoalsDisabled_ShouldWork()
    {
        // Act
        var format = new HomeAndAwayFormat(PeriodFormat.Default, UseAwayGoals: false);

        // Assert
        format.UseAwayGoals.Should().BeFalse();
        format.AllowDraw().Should().BeTrue();
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }

    [Fact]
    public void CreateHomeAndAwayFormat_WithExtraTimeAndAwayGoals_ShouldWork()
    {
        // Arrange
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());

        // Act
        var format = new HomeAndAwayFormat(PeriodFormat.Default, extraTime, UseAwayGoals: true);

        // Assert
        format.ExtraTime.Should().Be(extraTime);
        format.UseAwayGoals.Should().BeTrue();
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateHomeAndAwayFormat_WithAllOptionsEnabled_ShouldWork()
    {
        // Arrange
        var extraTime = PeriodFormat.ExtraTime;
        const bool useAwayGoals = true;
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new HomeAndAwayFormat(PeriodFormat.Default, extraTime, useAwayGoals, numberOfPenaltyShootouts);

        // Assert
        format.ExtraTime.Should().Be(extraTime);
        format.UseAwayGoals.Should().BeTrue();
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
        format.AllowDraw().Should().BeTrue();
        format.Type.Should().Be(RoundFormatType.HomeAndAway);
    }
}
