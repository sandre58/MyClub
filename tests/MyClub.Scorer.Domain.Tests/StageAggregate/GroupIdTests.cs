// -----------------------------------------------------------------------
// <copyright file="GroupIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.StageAggregate;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.StageAggregate;

public class GroupIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateGroupId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var groupId = new GroupId(guid);

        // Assert
        groupId.Value.Should().Be(guid);
    }

    [Fact]
    public void New_ShouldCreateUniqueGroupIds()
    {
        // Act
        var groupId1 = GroupId.New();
        var groupId2 = GroupId.New();

        // Assert
        groupId1.Should().NotBe(groupId2);
        groupId1.Value.Should().NotBe(groupId2.Value);
        groupId1.Value.Should().NotBe(Guid.Empty);
        groupId2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId1 = new GroupId(guid);
        var groupId2 = new GroupId(guid);

        // Act & Assert
        groupId1.Should().Be(groupId2);
        groupId1.Equals(groupId2).Should().BeTrue();
        (groupId1 == groupId2).Should().BeTrue();
        (groupId1 != groupId2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var groupId1 = new GroupId(Guid.NewGuid());
        var groupId2 = new GroupId(Guid.NewGuid());

        // Act & Assert
        groupId1.Should().NotBe(groupId2);
        groupId1.Equals(groupId2).Should().BeFalse();
        (groupId1 == groupId2).Should().BeFalse();
        (groupId1 != groupId2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId1 = new GroupId(guid);
        var groupId2 = new GroupId(guid);

        // Act & Assert
        groupId1.GetHashCode().Should().Be(groupId2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldBeDifferent()
    {
        // Arrange
        var groupId1 = new GroupId(Guid.NewGuid());
        var groupId2 = new GroupId(Guid.NewGuid());

        // Act & Assert
        groupId1.GetHashCode().Should().NotBe(groupId2.GetHashCode());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId = new GroupId(guid);

        // Act
        Guid result = groupId;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void Empty_ShouldCreateGroupIdWithEmptyGuid()
    {
        // Act
        var groupId = GroupId.Empty;

        // Assert
        groupId.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId1 = new GroupId(guid);
        var groupId2 = new GroupId(guid);
        var groupId3 = new GroupId(Guid.NewGuid());

        // Act & Assert
        groupId1.Should().Be(groupId2);
        groupId1.Should().NotBe(groupId3);
        groupId2.Should().NotBe(groupId3);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithDifferentValue()
    {
        // Arrange
        var originalGuid = Guid.NewGuid();
        var newGuid = Guid.NewGuid();
        var originalGroupId = new GroupId(originalGuid);

        // Act
        var newGroupId = originalGroupId with { Value = newGuid };

        // Assert
        newGroupId.Value.Should().Be(newGuid);
        originalGroupId.Value.Should().Be(originalGuid);
        newGroupId.Should().NotBe(originalGroupId);
    }
}
