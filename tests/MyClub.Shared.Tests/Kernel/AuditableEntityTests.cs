// -----------------------------------------------------------------------
// <copyright file="AuditableEntityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FluentAssertions;
using MyClub.Shared.Kernel.Events;
using MyClub.Shared.Kernel.Primitives;
using Xunit;

namespace MyClub.Shared.Tests.Kernel;

public class AuditableEntityTests
{
    #region Test Helper Classes

    // Test EntityId implementation
    internal sealed record TestAuditableEntityId(Guid Value) : EntityId<TestAuditableEntityId>(Value)
    {
        public static new TestAuditableEntityId New() => EntityId.New<TestAuditableEntityId>();
    }

    // Test AuditableEntity implementation
    private sealed class TestAuditableEntity : AuditableEntity<TestAuditableEntityId>
    {
        public TestAuditableEntity(TestAuditableEntityId id, string name)
            : base(id) => Name = name;

        public TestAuditableEntity() => Name = string.Empty;

        public string Name { get; }

        public void AddTestDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    // Test Domain Event implementation
    private sealed record TestDomainEvent(string Message) : IDomainEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTime.UtcNow;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_ShouldSetIdCorrectlyAndInitializeAuditProperties()
    {
        // Arrange
        var id = TestAuditableEntityId.New();
        const string name = "Test Auditable Entity";

        // Act
        var entity = new TestAuditableEntity(id, name);

        // Assert
        entity.Id.Should().Be(id);
        entity.Name.Should().Be(name);
        entity.DomainEvents.Should().BeEmpty();
        entity.CreatedAt.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateEntityWithDefaultIdAndInitializeAuditProperties()
    {
        // Act
        var entity = new TestAuditableEntity();

        // Assert
        // Note: Default constructor creates entity with default! ID (null) as used by EF Core
        entity.Id.Should().BeNull();
        entity.Name.Should().BeEmpty();
        entity.DomainEvents.Should().BeEmpty();
        entity.CreatedAt.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region Audit Properties Tests

    [Fact]
    public void CreatedAt_InitialValue_ShouldBeNull()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");

        // Assert
        entity.CreatedAt.Should().BeNull();
    }

    [Fact]
    public void CreatedBy_InitialValue_ShouldBeNull()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");

        // Assert
        entity.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void ModifiedAt_InitialValue_ShouldBeNull()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");

        // Assert
        entity.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public void ModifiedBy_InitialValue_ShouldBeNull()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");

        // Assert
        entity.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region MarkedAsCreated Tests

    [Fact]
    public void MarkedAsCreated_WithDateAndUser_ShouldSetCreatedPropertiesAndClearModifiedProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var createdAt = DateTime.UtcNow;
        const string createdBy = "TestUser";

        // Pre-populate modified properties to ensure they are cleared
        entity.MarkedAsModified(DateTime.UtcNow.AddMinutes(-10), "ModifiedUser");

        // Act
        entity.MarkedAsCreated(createdAt, createdBy);

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().Be(createdBy);
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkedAsCreated_WithDateOnly_ShouldSetCreatedAtAndClearModifiedProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var createdAt = DateTime.UtcNow;

        // Pre-populate modified properties to ensure they are cleared
        entity.MarkedAsModified(DateTime.UtcNow.AddMinutes(-10), "ModifiedUser");

        // Act
        entity.MarkedAsCreated(createdAt);

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().BeNull();
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkedAsCreated_WithNullDate_ShouldSetCreatedAtToNullAndClearModifiedProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        const string createdBy = "TestUser";

        // Pre-populate modified properties to ensure they are cleared
        entity.MarkedAsModified(DateTime.UtcNow.AddMinutes(-10), "ModifiedUser");

        // Act
        entity.MarkedAsCreated(null, createdBy);

        // Assert
        entity.CreatedAt.Should().BeNull();
        entity.CreatedBy.Should().Be(createdBy);
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkedAsCreated_WithNullUser_ShouldSetCreatedByToNull()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var createdAt = DateTime.UtcNow;

        // Act
        entity.MarkedAsCreated(createdAt);

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().BeNull();
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkedAsCreated_CalledMultipleTimes_ShouldOverwritePreviousValues()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var firstCreatedAt = DateTime.UtcNow.AddDays(-1);
        const string firstCreatedBy = "FirstUser";
        var secondCreatedAt = DateTime.UtcNow;
        const string secondCreatedBy = "SecondUser";

        // Act
        entity.MarkedAsCreated(firstCreatedAt, firstCreatedBy);
        entity.MarkedAsCreated(secondCreatedAt, secondCreatedBy);

        // Assert
        entity.CreatedAt.Should().Be(secondCreatedAt);
        entity.CreatedBy.Should().Be(secondCreatedBy);
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    #endregion

    #region MarkedAsModified Tests

    [Fact]
    public void MarkedAsModified_WithDateAndUser_ShouldSetModifiedProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var modifiedAt = DateTime.UtcNow;
        const string modifiedBy = "TestUser";

        // Act
        entity.MarkedAsModified(modifiedAt, modifiedBy);

        // Assert
        entity.ModifiedAt.Should().Be(modifiedAt);
        entity.ModifiedBy.Should().Be(modifiedBy);
    }

    [Fact]
    public void MarkedAsModified_WithDateOnly_ShouldSetModifiedAtAndModifiedByToNull()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var modifiedAt = DateTime.UtcNow;

        // Act
        entity.MarkedAsModified(modifiedAt);

        // Assert
        entity.ModifiedAt.Should().Be(modifiedAt);
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void MarkedAsModified_WithNullDate_ShouldSetModifiedAtToNull()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        const string modifiedBy = "TestUser";

        // Act
        entity.MarkedAsModified(null, modifiedBy);

        // Assert
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().Be(modifiedBy);
    }

    [Fact]
    public void MarkedAsModified_CalledMultipleTimes_ShouldOverwritePreviousValues()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var firstModifiedAt = DateTime.UtcNow.AddDays(-1);
        const string firstModifiedBy = "FirstUser";
        var secondModifiedAt = DateTime.UtcNow;
        const string secondModifiedBy = "SecondUser";

        // Act
        entity.MarkedAsModified(firstModifiedAt, firstModifiedBy);
        entity.MarkedAsModified(secondModifiedAt, secondModifiedBy);

        // Assert
        entity.ModifiedAt.Should().Be(secondModifiedAt);
        entity.ModifiedBy.Should().Be(secondModifiedBy);
    }

    [Fact]
    public void MarkedAsModified_DoesNotAffectCreatedProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var createdAt = DateTime.UtcNow.AddDays(-1);
        const string createdBy = "Creator";
        var modifiedAt = DateTime.UtcNow;
        const string modifiedBy = "Modifier";

        entity.MarkedAsCreated(createdAt, createdBy);

        // Act
        entity.MarkedAsModified(modifiedAt, modifiedBy);

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().Be(createdBy);
        entity.ModifiedAt.Should().Be(modifiedAt);
        entity.ModifiedBy.Should().Be(modifiedBy);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void AuditableEntity_ShouldInheritFromEntity()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");

        // Assert
        entity.Should().BeAssignableTo<Entity<TestAuditableEntityId>>();
        entity.Should().BeAssignableTo<IEntity<TestAuditableEntityId>>();
        entity.Should().BeAssignableTo<IAuditableEntity<TestAuditableEntityId>>();
    }

    [Fact]
    public void AuditableEntity_ShouldHaveAllEntityProperties()
    {
        // Arrange
        var id = TestAuditableEntityId.New();
        var entity = new TestAuditableEntity(id, "Test");

        // Act & Assert
        entity.Id.Should().Be(id);
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AuditableEntity_ShouldSupportDomainEvents()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test event");

        // Act
        entity.AddTestDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.Should().Contain(domainEvent);
        entity.DomainEvents.OfType<TestDomainEvent>().First().Message.Should().Be("Test event");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_ShouldBeBasedOnId()
    {
        // Arrange
        var id = TestAuditableEntityId.New();
        var entity1 = new TestAuditableEntity(id, "Entity1");
        var entity2 = new TestAuditableEntity(id, "Entity2");

        // Set different audit properties
        entity1.MarkedAsCreated(DateTime.UtcNow, "User1");
        entity2.MarkedAsModified(DateTime.UtcNow, "User2");

        // Act & Assert
        entity1.Should().Be(entity2);
        entity1.Equals(entity2).Should().BeTrue();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(TestAuditableEntityId.New(), "Entity1");
        var entity2 = new TestAuditableEntity(TestAuditableEntityId.New(), "Entity2");

        // Set same audit properties
        var now = DateTime.UtcNow;
        entity1.MarkedAsCreated(now, "User");
        entity2.MarkedAsCreated(now, "User");

        // Act & Assert
        entity1.Should().NotBe(entity2);
        entity1.Equals(entity2).Should().BeFalse();
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldIncludeIdAndClassName()
    {
        // Arrange
        var id = TestAuditableEntityId.New();
        var entity = new TestAuditableEntity(id, "Test");

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("TestAuditableEntity");
        result.Should().Contain(id.ToString());
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void AuditProperties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var entityType = typeof(TestAuditableEntity);

        // Assert
        entityType.GetProperty(nameof(TestAuditableEntity.CreatedAt))!.CanWrite.Should().BeFalse();
        entityType.GetProperty(nameof(TestAuditableEntity.CreatedBy))!.CanWrite.Should().BeFalse();
        entityType.GetProperty(nameof(TestAuditableEntity.ModifiedAt))!.CanWrite.Should().BeFalse();
        entityType.GetProperty(nameof(TestAuditableEntity.ModifiedBy))!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void MarkedAsCreated_WithEmptyString_ShouldSetCreatedByToEmptyString()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var createdAt = DateTime.UtcNow;

        // Act
        entity.MarkedAsCreated(createdAt, string.Empty);

        // Assert
        entity.CreatedAt.Should().Be(createdAt);
        entity.CreatedBy.Should().Be(string.Empty);
    }

    [Fact]
    public void MarkedAsModified_WithEmptyString_ShouldSetModifiedByToEmptyString()
    {
        // Arrange
        var entity = new TestAuditableEntity(TestAuditableEntityId.New(), "Test");
        var modifiedAt = DateTime.UtcNow;

        // Act
        entity.MarkedAsModified(modifiedAt, string.Empty);

        // Assert
        entity.ModifiedAt.Should().Be(modifiedAt);
        entity.ModifiedBy.Should().Be(string.Empty);
    }

    [Fact]
    public void AuditMethods_ShouldBeVirtual()
    {
        // Arrange & Act
        var entityType = typeof(AuditableEntity<TestAuditableEntityId>);

        // Assert
        entityType.GetMethod(nameof(AuditableEntity<>.MarkedAsCreated))!.IsVirtual.Should().BeTrue();
        entityType.GetMethod(nameof(AuditableEntity<>.MarkedAsModified))!.IsVirtual.Should().BeTrue();
    }

    #endregion
}
