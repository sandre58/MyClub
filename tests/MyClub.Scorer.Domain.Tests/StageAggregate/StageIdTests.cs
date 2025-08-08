// -----------------------------------------------------------------------
// <copyright file="StageIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.StageAggregate;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class StageIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateStageId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var stageId = new StageId(guid);

        // Assert
        stageId.Value.Should().Be(guid);
    }

    [Fact]
    public void New_ShouldCreateUniqueStageIds()
    {
        // Act
        var stageId1 = StageId.New();
        var stageId2 = StageId.New();

        // Assert
        stageId1.Should().NotBe(stageId2);
        stageId1.Value.Should().NotBe(stageId2.Value);
        stageId1.Value.Should().NotBe(Guid.Empty);
        stageId2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var stageId1 = new StageId(guid);
        var stageId2 = new StageId(guid);

        // Act & Assert
        stageId1.Should().Be(stageId2);
        stageId1.Equals(stageId2).Should().BeTrue();
        (stageId1 == stageId2).Should().BeTrue();
        (stageId1 != stageId2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var stageId1 = new StageId(Guid.NewGuid());
        var stageId2 = new StageId(Guid.NewGuid());

        // Act & Assert
        stageId1.Should().NotBe(stageId2);
        stageId1.Equals(stageId2).Should().BeFalse();
        (stageId1 == stageId2).Should().BeFalse();
        (stageId1 != stageId2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var stageId1 = new StageId(guid);
        var stageId2 = new StageId(guid);

        // Act & Assert
        stageId1.GetHashCode().Should().Be(stageId2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldBeDifferent()
    {
        // Arrange
        var stageId1 = new StageId(Guid.NewGuid());
        var stageId2 = new StageId(Guid.NewGuid());

        // Act & Assert
        stageId1.GetHashCode().Should().NotBe(stageId2.GetHashCode());
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var stageId = new StageId(guid);

        // Act
        var value = stageId.Value;

        // Assert
        value.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var stageId = new StageId(guid);

        // Act
        Guid result = stageId;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void Empty_ShouldCreateStageIdWithEmptyGuid()
    {
        // Act
        var stageId = StageId.Empty;

        // Assert
        stageId.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var stageId1 = new StageId(guid);
        var stageId2 = new StageId(guid);
        var stageId3 = new StageId(Guid.NewGuid());

        // Act & Assert
        stageId1.Should().Be(stageId2);
        stageId1.Should().NotBe(stageId3);
        stageId2.Should().NotBe(stageId3);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithDifferentValue()
    {
        // Arrange
        var originalGuid = Guid.NewGuid();
        var newGuid = Guid.NewGuid();
        var originalStageId = new StageId(originalGuid);

        // Act
        var newStageId = originalStageId with { Value = newGuid };

        // Assert
        newStageId.Value.Should().Be(newGuid);
        originalStageId.Value.Should().Be(originalGuid);
        newStageId.Should().NotBe(originalStageId);
    }
}
