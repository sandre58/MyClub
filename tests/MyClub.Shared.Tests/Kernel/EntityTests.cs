// -----------------------------------------------------------------------
// <copyright file="EntityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MyClub.Shared.Kernel.Events;
using MyClub.Shared.Kernel.Primitives;
using Xunit;

namespace MyClub.Shared.Tests.Kernel;

public class EntityTests
{
    #region Test Helper Classes

    // Test EntityId implementation
    internal sealed record TestEntityId(Guid Value) : EntityId<TestEntityId>(Value)
    {
        public static new TestEntityId New() => EntityId.New<TestEntityId>();
    }

    // Test Entity implementation
    private sealed class TestEntity(TestEntityId id, string name) : Entity<TestEntityId>(id)
    {
        public string Name { get; set; } = name;

        public void AddTestDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);

        public void RemoveTestDomainEvent(IDomainEvent domainEvent) => RemoveDomainEvent(domainEvent);
    }

    // Test Domain Event implementation
    private sealed record TestDomainEvent(string Message) : IDomainEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTime.UtcNow;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_ShouldSetIdCorrectly()
    {
        // Arrange
        var id = TestEntityId.New();
        const string name = "Test Entity";

        // Act
        var entity = new TestEntity(id, name);

        // Assert
        entity.Id.Should().Be(id);
        entity.Name.Should().Be(name);
        entity.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void AddDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test message");

        // Act
        entity.AddTestDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().Contain(domainEvent);
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void AddDomainEvent_WithMultipleEvents_ShouldAddAllEvents()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Message 1");
        var event2 = new TestDomainEvent("Message 2");

        // Act
        entity.AddTestDomainEvent(event1);
        entity.AddTestDomainEvent(event2);

        // Assert
        entity.DomainEvents.Should().Contain(event1);
        entity.DomainEvents.Should().Contain(event2);
        entity.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveDomainEvent_ShouldRemoveEventFromCollection()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Message 1");
        var event2 = new TestDomainEvent("Message 2");

        entity.AddTestDomainEvent(event1);
        entity.AddTestDomainEvent(event2);

        // Act
        entity.RemoveTestDomainEvent(event1);

        // Assert
        entity.DomainEvents.Should().NotContain(event1);
        entity.DomainEvents.Should().Contain(event2);
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Message 1");
        var event2 = new TestDomainEvent("Message 2");

        entity.AddTestDomainEvent(event1);
        entity.AddTestDomainEvent(event2);

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test message");
        entity.AddTestDomainEvent(domainEvent);

        // Act
        var domainEvents = entity.DomainEvents;

        // Assert
        domainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2"); // Different name, same ID

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
        entity1.Equals((object)entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new TestEntity(TestEntityId.New(), "Entity 1");
        var entity2 = new TestEntity(TestEntityId.New(), "Entity 1"); // Same name, different ID

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
        entity1.Equals((object)entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");

        // Act & Assert
        entity.Equals(entity).Should().BeTrue();
        entity.Equals((object)entity).Should().BeTrue();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2");

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentId_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var entity1 = new TestEntity(TestEntityId.New(), "Entity");
        var entity2 = new TestEntity(TestEntityId.New(), "Entity");

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");

        // Act
        var hashCode1 = entity.GetHashCode();
        var hashCode2 = entity.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity = new TestEntity(id, "Test");

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Be($"TestEntity [{id}]");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Id_ShouldBeReadOnly()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity = new TestEntity(id, "Test");

        // Act & Assert
        entity.Id.Should().Be(id);

        // Verify that ID property doesn't have a setter (compile-time check)
        typeof(TestEntity).GetProperty(nameof(TestEntity.Id))!.CanWrite.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Entity_WithSameIdButDifferentProperties_ShouldBeEqual()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = new TestEntity(id, "Original Name");
        var entity2 = new TestEntity(id, "Different Name");

        // Modify properties after creation
        entity1.Name = "Modified Name 1";
        entity2.Name = "Modified Name 2";

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue("Entities are equal based on Id only, not properties");
    }

    [Fact]
    public void DomainEvents_ModificationAfterRetrieval_ShouldNotAffectOriginal()
    {
        // Arrange
        var entity = new TestEntity(TestEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test");
        entity.AddTestDomainEvent(domainEvent);

        // Act
        var domainEvents = entity.DomainEvents;

        // Assert
        domainEvents.Should().HaveCount(1);
        domainEvents.OfType<TestDomainEvent>().First().Message.Should().Be("Test");

        // Verify it's truly read-only (should not be able to cast to a mutable collection)
        var act = () => ((ICollection<IDomainEvent>)domainEvents).Add(new TestDomainEvent("Should fail"));
        act.Should().Throw<NotSupportedException>();
    }

    #endregion
}
