// -----------------------------------------------------------------------
// <copyright file="RoundFormatTypeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FluentAssertions;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.RoundAggregate.Format;

public class RoundFormatTypeTests
{
    [Fact]
    public void RoundFormatType_ShouldHaveCorrectValues()
    {
        // Act & Assert
        Enum.GetValues<RoundFormatType>().Should().HaveCount(4);
        Enum.GetValues<RoundFormatType>().Should().Contain(RoundFormatType.Single);
        Enum.GetValues<RoundFormatType>().Should().Contain(RoundFormatType.HomeAndAway);
        Enum.GetValues<RoundFormatType>().Should().Contain(RoundFormatType.BestOf);
        Enum.GetValues<RoundFormatType>().Should().Contain(RoundFormatType.Replay);
    }

    [Fact]
    public void RoundFormatType_Single_ShouldHaveCorrectValue()
    {
        // Act & Assert
        ((int)RoundFormatType.Single).Should().Be(0);
        nameof(RoundFormatType.Single).Should().Be("Single");
    }

    [Fact]
    public void RoundFormatType_HomeAndAway_ShouldHaveCorrectValue()
    {
        // Act & Assert
        ((int)RoundFormatType.HomeAndAway).Should().Be(1);
        nameof(RoundFormatType.HomeAndAway).Should().Be("HomeAndAway");
    }

    [Fact]
    public void RoundFormatType_BestOf_ShouldHaveCorrectValue()
    {
        // Act & Assert
        ((int)RoundFormatType.BestOf).Should().Be(2);
        nameof(RoundFormatType.BestOf).Should().Be("BestOf");
    }

    [Fact]
    public void RoundFormatType_Replay_ShouldHaveCorrectValue()
    {
        // Act & Assert
        ((int)RoundFormatType.Replay).Should().Be(3);
        nameof(RoundFormatType.Replay).Should().Be("Replay");
    }

    [Fact]
    public void RoundFormatType_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allValues = Enum.GetValues<RoundFormatType>();

        // Act
        var uniqueValues = allValues.Distinct().ToArray();

        // Assert
        uniqueValues.Should().HaveCount(allValues.Length);
    }

    [Fact]
    public void RoundFormatType_AllValues_ShouldHaveValidNames()
    {
        // Arrange
        var allValues = Enum.GetValues<RoundFormatType>();

        // Act & Assert
        foreach (var value in allValues)
        {
            value.ToString().Should().NotBeNullOrEmpty();
            Enum.IsDefined(value).Should().BeTrue();
        }
    }

    [Fact]
    public void RoundFormatType_Parse_ShouldWorkCorrectly()
    {
        // Act & Assert
        Enum.Parse<RoundFormatType>("Single").Should().Be(RoundFormatType.Single);
        Enum.Parse<RoundFormatType>("HomeAndAway").Should().Be(RoundFormatType.HomeAndAway);
        Enum.Parse<RoundFormatType>("BestOf").Should().Be(RoundFormatType.BestOf);
        Enum.Parse<RoundFormatType>("Replay").Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void RoundFormatType_TryParse_WithValidValue_ShouldReturnTrue()
    {
        // Act
        var result1 = Enum.TryParse<RoundFormatType>("Single", out var value1);
        var result2 = Enum.TryParse<RoundFormatType>("HomeAndAway", out var value2);
        var result3 = Enum.TryParse<RoundFormatType>("BestOf", out var value3);
        var result4 = Enum.TryParse<RoundFormatType>("Replay", out var value4);

        // Assert
        result1.Should().BeTrue();
        value1.Should().Be(RoundFormatType.Single);

        result2.Should().BeTrue();
        value2.Should().Be(RoundFormatType.HomeAndAway);

        result3.Should().BeTrue();
        value3.Should().Be(RoundFormatType.BestOf);

        result4.Should().BeTrue();
        value4.Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void RoundFormatType_TryParse_WithInvalidValue_ShouldReturnFalse()
    {
        // Act
        var result = Enum.TryParse<RoundFormatType>("InvalidValue", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact]
    public void RoundFormatType_TryParse_WithCaseInsensitive_ShouldWork()
    {
        // Act
        var result1 = Enum.TryParse<RoundFormatType>("single", true, out var value1);
        var result2 = Enum.TryParse<RoundFormatType>("HOMEANDAWAY", true, out var value2);
        var result3 = Enum.TryParse<RoundFormatType>("bestof", true, out var value3);
        var result4 = Enum.TryParse<RoundFormatType>("REPLAY", true, out var value4);

        // Assert
        result1.Should().BeTrue();
        value1.Should().Be(RoundFormatType.Single);

        result2.Should().BeTrue();
        value2.Should().Be(RoundFormatType.HomeAndAway);

        result3.Should().BeTrue();
        value3.Should().Be(RoundFormatType.BestOf);

        result4.Should().BeTrue();
        value4.Should().Be(RoundFormatType.Replay);
    }

    [Fact]
    public void RoundFormatType_GetName_ShouldReturnCorrectNames()
    {
        // Act & Assert
        Enum.GetName(RoundFormatType.Single).Should().Be("Single");
        Enum.GetName(RoundFormatType.HomeAndAway).Should().Be("HomeAndAway");
        Enum.GetName(RoundFormatType.BestOf).Should().Be("BestOf");
        Enum.GetName(RoundFormatType.Replay).Should().Be("Replay");
    }

    [Fact]
    public void RoundFormatType_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames<RoundFormatType>();

        // Assert
        names.Should().HaveCount(4);
        names.Should().Contain("Single");
        names.Should().Contain("HomeAndAway");
        names.Should().Contain("BestOf");
        names.Should().Contain("Replay");
    }

    [Fact]
    public void RoundFormatType_IsDefined_ShouldWorkCorrectly()
    {
        // Act & Assert
        Enum.IsDefined(RoundFormatType.Single).Should().BeTrue();
        Enum.IsDefined(RoundFormatType.HomeAndAway).Should().BeTrue();
        Enum.IsDefined(RoundFormatType.BestOf).Should().BeTrue();
        Enum.IsDefined(RoundFormatType.Replay).Should().BeTrue();
        Enum.IsDefined(typeof(RoundFormatType), (RoundFormatType)999).Should().BeFalse();
    }

    [Fact]
    public void RoundFormatType_Comparison_ShouldWorkCorrectly()
    {
        (RoundFormatType.Single < RoundFormatType.HomeAndAway).Should().BeTrue();
        (RoundFormatType.HomeAndAway < RoundFormatType.BestOf).Should().BeTrue();
        (RoundFormatType.BestOf < RoundFormatType.Replay).Should().BeTrue();
    }

    [Fact]
    public void RoundFormatType_HashCode_ShouldBeConsistent()
    {
        // Arrange
        const RoundFormatType value1 = RoundFormatType.Single;
        const RoundFormatType value2 = RoundFormatType.Single;
        const RoundFormatType value3 = RoundFormatType.HomeAndAway;

        // Act & Assert
        value1.GetHashCode().Should().Be(value2.GetHashCode());
        value1.GetHashCode().Should().NotBe(value3.GetHashCode());
    }
}
