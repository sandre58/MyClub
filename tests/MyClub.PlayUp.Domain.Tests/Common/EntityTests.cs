// -----------------------------------------------------------------------
// <copyright file="EntityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class EntityTests
{
    [Fact]
    public void Two_entities_with_the_same_id_and_type_are_equal()
    {
        // Arrange
        var id = CompetitionId.New();
        var left = new TestEntity(id);
        var right = new TestEntity(id);

        // Act / Assert
        left.Should().Be(right);
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Two_entities_with_different_ids_are_not_equal()
    {
        // Arrange
        var left = new TestEntity(CompetitionId.New());
        var right = new TestEntity(CompetitionId.New());

        // Act / Assert
        left.Should().NotBe(right);
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        // Arrange
        var id = CompetitionId.New();
        Entity<CompetitionId> left = new TestEntity(id);
        Entity<CompetitionId> right = new OtherTestEntity(id);

        // Act / Assert
        left.Equals(right).Should().BeFalse();
    }

    [Fact]
    public void Entity_is_not_equal_to_null()
    {
        // Arrange
        var entity = new TestEntity(CompetitionId.New());

        // Act / Assert
        AreEqual(entity, null).Should().BeFalse();
        AreEqual(null, entity).Should().BeFalse();
    }

    private static bool AreEqual(Entity<CompetitionId>? left, Entity<CompetitionId>? right) => left == right;
}
