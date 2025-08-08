// -----------------------------------------------------------------------
// <copyright file="ReplayFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyNet.Utilities;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class ReplayFormatTests
{
    [Fact]
    public void Constructor_WithValidRegulationTime_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        // Act
        var format = new ReplayFormat(regulationTime);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().BeNull();
        format.NumberOfPenaltyShootouts.Should().BeNull();
        format.Type.Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new ReplayFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().Be(extraTime);
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
        format.Type.Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void Type_ShouldReturnReplay()
    {
        // Arrange
        var format = new ReplayFormat(PeriodFormat.Default);

        // Act & Assert
        format.Type.Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void AllowDraw_ShouldReturnTrue()
    {
        // Arrange
        var format = new ReplayFormat(PeriodFormat.Default);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void AllowDraw_WithExtraTime_ShouldStillReturnTrue()
    {
        // Arrange
        var format = new ReplayFormat(PeriodFormat.Default, PeriodFormat.ExtraTime);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void AllowDraw_WithPenaltyShootouts_ShouldStillReturnTrue()
    {
        // Arrange
        var format = new ReplayFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;

        var format1 = new ReplayFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        var format2 = new ReplayFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

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

        var format1 = new ReplayFormat(regulationTime1);
        var format2 = new ReplayFormat(regulationTime2);

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

        var format1 = new ReplayFormat(regulationTime, extraTime1);
        var format2 = new ReplayFormat(regulationTime, extraTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void RecordEquality_WithDifferentNumberOfPenaltyShootouts_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        var format1 = new ReplayFormat(regulationTime, NumberOfPenaltyShootouts: 3);
        var format2 = new ReplayFormat(regulationTime, NumberOfPenaltyShootouts: 5);

        // Act & Assert
        format1.Should().NotBe(format2);
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var format = new ReplayFormat(PeriodFormat.Default);

        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("ReplayFormat");
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;
        var format = new ReplayFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

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
        var originalFormat = new ReplayFormat(PeriodFormat.Default);
        var newExtraTime = PeriodFormat.ExtraTime;

        // Act
        var modifiedFormat = originalFormat with { ExtraTime = newExtraTime };

        // Assert
        modifiedFormat.RegulationTime.Should().Be(originalFormat.RegulationTime);
        modifiedFormat.ExtraTime.Should().Be(newExtraTime);
        modifiedFormat.NumberOfPenaltyShootouts.Should().Be(originalFormat.NumberOfPenaltyShootouts);
        modifiedFormat.Type.Should().Be(RoundFormatType.Replay);
        modifiedFormat.AllowDraw().Should().BeTrue();

        // The Original should remain unchanged
        originalFormat.ExtraTime.Should().BeNull();
    }

    [Fact]
    public void CreateReplayFormat_WithDefaultPeriodFormat_ShouldWork()
    {
        // Act
        var format = new ReplayFormat(PeriodFormat.Default);

        // Assert
        format.RegulationTime.Should().Be(PeriodFormat.Default);
        format.RegulationTime.Number.Should().Be(2);
        format.RegulationTime.Duration.Should().Be(45.Minutes());
        format.RegulationTime.HalfTimeDuration.Should().Be(15.Minutes());
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_WithCustomPeriodFormat_ShouldWork()
    {
        // Arrange
        var customRegulationTime = new PeriodFormat(3, 30.Minutes(), 10.Minutes());

        // Act
        var format = new ReplayFormat(customRegulationTime);

        // Assert
        format.RegulationTime.Should().Be(customRegulationTime);
        format.RegulationTime.Number.Should().Be(3);
        format.RegulationTime.Duration.Should().Be(30.Minutes());
        format.RegulationTime.HalfTimeDuration.Should().Be(10.Minutes());
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_WithExtraTimeOnly_ShouldWork()
    {
        // Arrange
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());

        // Act
        var format = new ReplayFormat(PeriodFormat.Default, extraTime);

        // Assert
        format.ExtraTime.Should().Be(extraTime);
        format.NumberOfPenaltyShootouts.Should().BeNull();
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_WithPenaltyShootoutsOnly_ShouldWork()
    {
        // Act
        var format = new ReplayFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 5);

        // Assert
        format.ExtraTime.Should().BeNull();
        format.NumberOfPenaltyShootouts.Should().Be(5);
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_WithZeroPenaltyShootouts_ShouldWork()
    {
        // Act
        var format = new ReplayFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: 0);

        // Assert
        format.NumberOfPenaltyShootouts.Should().Be(0);
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_WithNegativePenaltyShootouts_ShouldWork()
    {
        // Act
        var format = new ReplayFormat(PeriodFormat.Default, NumberOfPenaltyShootouts: -1);

        // Assert
        format.NumberOfPenaltyShootouts.Should().Be(-1);
        format.AllowDraw().Should().BeTrue();
    }

    [Fact]
    public void CreateReplayFormat_CompareWithSingleFormat_ShouldHaveDifferentAllowDrawBehavior()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        // Act
        var replayFormat = new ReplayFormat(regulationTime);
        var singleFormat = new SingleFormat(regulationTime);

        // Assert
        replayFormat.AllowDraw().Should().BeTrue();
        singleFormat.AllowDraw().Should().BeFalse();
        replayFormat.Type.Should().Be(RoundFormatType.Replay);
        singleFormat.Type.Should().Be(RoundFormatType.Single);
    }

    [Fact]
    public void CreateReplayFormat_CompareWithHomeAndAwayFormat_ShouldHaveSameAllowDrawBehavior()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;

        // Act
        var replayFormat = new ReplayFormat(regulationTime);
        var homeAndAwayFormat = new HomeAndAwayFormat(regulationTime);

        // Assert
        replayFormat.AllowDraw().Should().BeTrue();
        homeAndAwayFormat.AllowDraw().Should().BeTrue();
        replayFormat.Type.Should().Be(RoundFormatType.Replay);
        homeAndAwayFormat.Type.Should().Be(RoundFormatType.HomeAndAway);
    }
}
