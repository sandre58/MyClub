// -----------------------------------------------------------------------
// <copyright file="SingleFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyNet.Utilities;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class SingleFormatTests
{
    [Fact]
    public void Constructor_WithValidRegulationTime_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        // Act
        var format = new SingleFormat(regulationTime);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().BeNull();
        format.NumberOfPenaltyShootouts.Should().BeNull();
        format.Type.Should().Be(RoundFormatType.Single);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new SingleFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().Be(extraTime);
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
        format.Type.Should().Be(RoundFormatType.Single);
    }

    [Fact]
    public void Type_ShouldReturnSingle()
    {
        // Arrange
        var format = new SingleFormat(PeriodFormat.Default);

        // Act & Assert
        format.Type.Should().Be(RoundFormatType.Single);
    }

    [Fact]
    public void AllowDraw_ShouldReturnFalse()
    {
        // Arrange
        var format = new SingleFormat(PeriodFormat.Default);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void AllowDraw_WithExtraTime_ShouldStillReturnFalse()
    {
        // Arrange
        var format = new SingleFormat(PeriodFormat.Default, PeriodFormat.ExtraTime);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void AllowDraw_WithPenaltyShootouts_ShouldStillReturnFalse()
    {
        // Arrange
        var format = new SingleFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;

        var format1 = new SingleFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        var format2 = new SingleFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

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

        var format1 = new SingleFormat(regulationTime1);
        var format2 = new SingleFormat(regulationTime2);

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

        var format1 = new SingleFormat(regulationTime, extraTime1);
        var format2 = new SingleFormat(regulationTime, extraTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentNumberOfPenaltyShootouts_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        var format1 = new SingleFormat(regulationTime, NumberOfPenaltyShootouts: 3);
        var format2 = new SingleFormat(regulationTime, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var format = new SingleFormat(PeriodFormat.Default);

        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("SingleFormat");
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;
        var format = new SingleFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

        // Act
        var (actualRegulationTime, actualExtraTime, actualNumberOfPenaltyShootouts) = format;

        // Assert
        actualRegulationTime.Should().Be(regulationTime);
        actualExtraTime.Should().Be(extraTime);
        actualNumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var originalFormat = new SingleFormat(PeriodFormat.Default);
        var newExtraTime = PeriodFormat.ExtraTime;

        // Act
        var modifiedFormat = originalFormat with { ExtraTime = newExtraTime };

        // Assert
        modifiedFormat.RegulationTime.Should().Be(originalFormat.RegulationTime);
        modifiedFormat.ExtraTime.Should().Be(newExtraTime);
        modifiedFormat.NumberOfPenaltyShootouts.Should().Be(originalFormat.NumberOfPenaltyShootouts);
        modifiedFormat.Type.Should().Be(RoundFormatType.Single);
        modifiedFormat.AllowDraw().Should().BeFalse();

        // The Original should remain unchanged
        originalFormat.ExtraTime.Should().BeNull();
    }

    [Fact]
    public void CreateSingleFormat_WithDefaultPeriodFormat_ShouldWork()
    {
        // Act
        var format = new SingleFormat(PeriodFormat.Default);

        // Assert
        format.RegulationTime.Should().Be(PeriodFormat.Default);
        format.RegulationTime.Number.Should().Be(2);
        format.RegulationTime.Duration.Should().Be(45.Minutes());
        format.RegulationTime.HalfTimeDuration.Should().Be(15.Minutes());
    }

    [Fact]
    public void CreateSingleFormat_WithCustomPeriodFormat_ShouldWork()
    {
        // Arrange
        var customRegulationTime = new PeriodFormat(3, 30.Minutes(), 10.Minutes());

        // Act
        var format = new SingleFormat(customRegulationTime);

        // Assert
        format.RegulationTime.Should().Be(customRegulationTime);
        format.RegulationTime.Number.Should().Be(3);
        format.RegulationTime.Duration.Should().Be(30.Minutes());
        format.RegulationTime.HalfTimeDuration.Should().Be(10.Minutes());
    }

    [Fact]
    public void CreateSingleFormat_WithZeroPenaltyShootouts_ShouldWork()
    {
        // Act
        var format = new SingleFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 0);

        // Assert
        format.NumberOfPenaltyShootouts.Should().Be(0);
        format.AllowDraw().Should().BeFalse();
    }

    [Fact]
    public void CreateSingleFormat_WithNegativePenaltyShootouts_ShouldWork()
    {
        // Act
        var format = new SingleFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: -1);

        // Assert
        format.NumberOfPenaltyShootouts.Should().Be(-1);
        format.AllowDraw().Should().BeFalse();
    }
}
