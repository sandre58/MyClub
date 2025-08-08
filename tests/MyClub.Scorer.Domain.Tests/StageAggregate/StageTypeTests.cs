// -----------------------------------------------------------------------
// <copyright file="StageTypeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.StageAggregate;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class StageTypeTests
{
    [Fact]
    public void StageType_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<StageType>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(StageType.Knockout);
        values.Should().Contain(StageType.Championship);
        values.Should().Contain(StageType.Groups);
    }

    [Theory]
    [InlineData(StageType.Knockout, "Knockout")]
    [InlineData(StageType.Championship, "Championship")]
    [InlineData(StageType.Groups, "Groups")]
    public void ToString_ShouldReturnCorrectStringRepresentation(StageType stageType, string expected)
    {
        // Act
        var result = stageType.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Knockout", StageType.Knockout)]
    [InlineData("Championship", StageType.Championship)]
    [InlineData("Groups", StageType.Groups)]
    public void Parse_WithValidString_ShouldReturnCorrectEnum(string input, StageType expected)
    {
        // Act
        var result = Enum.Parse<StageType>(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("knockout")]
    [InlineData("KNOCKOUT")]
    [InlineData("championship")]
    [InlineData("CHAMPIONSHIP")]
    [InlineData("groups")]
    [InlineData("GROUPS")]
    public void Parse_WithIgnoreCase_ShouldReturnCorrectEnum(string input)
    {
        // Act
        var result = Enum.Parse<StageType>(input, true);

        // Assert
        result.Should().BeOneOf(StageType.Knockout, StageType.Championship, StageType.Groups);
    }

    [Fact]
    public void Parse_WithNullString_ShouldThrowArgumentNullException() => Assert.Throws<ArgumentNullException>(static () => Enum.Parse<StageType>(null!));

    [Theory]
    [InlineData("Knockout", true)]
    [InlineData("Championship", true)]
    [InlineData("Groups", true)]
    [InlineData("InvalidStageType", false)]
    [InlineData("", false)]
    public void TryParse_ShouldReturnExpectedResult(string input, bool expectedSuccess)
    {
        // Act
        var success = Enum.TryParse<StageType>(input, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Should().BeOneOf(StageType.Knockout, StageType.Championship, StageType.Groups);
        }
    }

    [Theory]
    [InlineData(StageType.Knockout)]
    [InlineData(StageType.Championship)]
    [InlineData(StageType.Groups)]
    public void IsDefined_WithValidValues_ShouldReturnTrue(StageType stageType)
    {
        // Act
        var result = Enum.IsDefined(stageType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefined_WithInvalidValue_ShouldReturnFalse()
    {
        // Arrange
        const StageType invalidStageType = (StageType)999;

        // Act
        var result = Enum.IsDefined(invalidStageType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetNames_ShouldReturnAllStageTypeNames()
    {
        // Act
        var names = Enum.GetNames<StageType>();

        // Assert
        names.Should().HaveCount(3);
        names.Should().Contain("Knockout");
        names.Should().Contain("Championship");
        names.Should().Contain("Groups");
    }

    [Fact]
    public void DefaultValue_ShouldBeKnockout()
    {
        // Act
        const StageType defaultValue = default;

        // Assert
        defaultValue.Should().Be(StageType.Knockout);
    }

    [Theory]
    [InlineData(StageType.Knockout, StageType.Knockout, true)]
    [InlineData(StageType.Championship, StageType.Championship, true)]
    [InlineData(StageType.Groups, StageType.Groups, true)]
    [InlineData(StageType.Knockout, StageType.Championship, false)]
    [InlineData(StageType.Championship, StageType.Groups, false)]
    [InlineData(StageType.Groups, StageType.Knockout, false)]
    public void Equality_ShouldWorkCorrectly(StageType first, StageType second, bool expectedEqual)
    {
        // Act & Assert
        (first == second).Should().Be(expectedEqual);
        (first != second).Should().Be(!expectedEqual);
        first.Equals(second).Should().Be(expectedEqual);
    }

    [Fact]
    public void GetHashCode_ForSameValues_ShouldBeEqual()
    {
        // Arrange
        const StageType stageType1 = StageType.Championship;
        const StageType stageType2 = StageType.Championship;

        // Act & Assert
        stageType1.GetHashCode().Should().Be(stageType2.GetHashCode());
    }
}
