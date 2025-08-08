// -----------------------------------------------------------------------
// <copyright file="RoundFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyNet.Utilities;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class RoundFormatTests
{
    private sealed record TestableRoundFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
    {
        public override RoundFormatType Type => RoundFormatType.Single;

        public override bool AllowDraw() => false;
    }

    [Fact]
    public void Constructor_WithValidRegulationTime_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());

        // Act
        var format = new TestableRoundFormat(regulationTime);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().BeNull();
        format.NumberOfPenaltyShootouts.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var regulationTime = new PeriodFormat(2, 45.Minutes(), 15.Minutes());
        var extraTime = new PeriodFormat(2, 15.Minutes(), 5.Minutes());
        const int numberOfPenaltyShootouts = 5;

        // Act
        var format = new TestableRoundFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

        // Assert
        format.RegulationTime.Should().Be(regulationTime);
        format.ExtraTime.Should().Be(extraTime);
        format.NumberOfPenaltyShootouts.Should().Be(numberOfPenaltyShootouts);
    }

    [Fact]
    public void Type_ShouldBeImplementedByConcreteClass()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var format = new TestableRoundFormat(regulationTime);

        // Act & Assert
        format.Type.Should().Be(RoundFormatType.Single);
    }

    [Fact]
    public void AllowDraw_ShouldBeImplementedByConcreteClass()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var format = new TestableRoundFormat(regulationTime);

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

        var format1 = new TestableRoundFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        var format2 = new TestableRoundFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

        // Act & Assert
        format1.Should().Be(format2);
        format1.GetHashCode().Should().Be(format2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var regulationTime1 = PeriodFormat.Default;
        var regulationTime2 = new PeriodFormat(2, 40.Minutes(), 10.Minutes());

        var format1 = new TestableRoundFormat(regulationTime1);
        var format2 = new TestableRoundFormat(regulationTime2);

        // Act & Assert
        format1.Should().NotBe(format2);
        format1.GetHashCode().Should().NotBe(format2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var format = new TestableRoundFormat(regulationTime);

        // Act
        var result = format.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TestableRoundFormat");
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var regulationTime = PeriodFormat.Default;
        var extraTime = PeriodFormat.ExtraTime;
        const int numberOfPenaltyShootouts = 5;
        var format = new TestableRoundFormat(regulationTime, extraTime, numberOfPenaltyShootouts);

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
        var originalFormat = new TestableRoundFormat(PeriodFormat.Default);
        var newExtraTime = PeriodFormat.ExtraTime;

        // Act
        var modifiedFormat = originalFormat with { ExtraTime = newExtraTime };

        // Assert
        modifiedFormat.RegulationTime.Should().Be(originalFormat.RegulationTime);
        modifiedFormat.ExtraTime.Should().Be(newExtraTime);
        modifiedFormat.NumberOfPenaltyShootouts.Should().Be(originalFormat.NumberOfPenaltyShootouts);

        // The Original should remain unchanged
        originalFormat.ExtraTime.Should().BeNull();
    }
}
