// -----------------------------------------------------------------------
// <copyright file="EntityLink.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Abstract base class for Entity Framework Core join entities representing many-to-many relationships
/// between entities with strongly-typed identifiers. This class provides the fundamental structure
/// for explicit join tables in complex domain relationships.
/// </summary>
/// <typeparam name="TLeftId">The strongly-typed identifier for the left side entity.</typeparam>
/// <typeparam name="TRightId">The strongly-typed identifier for the right side entity.</typeparam>
/// <param name="leftId">The identifier of the left side entity in the relationship.</param>
/// <param name="rightId">The identifier of the right side entity in the relationship.</param>
public abstract class EntityLink<TLeftId, TRightId>(TLeftId leftId, TRightId rightId)
    where TLeftId : EntityId<TLeftId>
    where TRightId : EntityId<TRightId>
{
    /// <summary>
    /// Gets or sets the identifier of the left side entity in the relationship.
    /// This property represents the "from" side of the many-to-many relationship.
    /// </summary>
    public TLeftId LeftId { get; set; } = leftId;

    /// <summary>
    /// Gets or sets the identifier of the right side entity in the relationship.
    /// This property represents the "to" side of the many-to-many relationship.
    /// </summary>
    public TRightId RightId { get; set; } = rightId;
}

/// <summary>
/// Extended join entity class that includes a navigation property to the right side entity.
/// This variation is used when the join entity needs direct access to the related entity
/// for complex operations or when eager loading is beneficial.
/// </summary>
/// <typeparam name="TRight">The entity type for the right side of the relationship.</typeparam>
/// <typeparam name="TLeftId">The strongly-typed identifier for the left side entity.</typeparam>
/// <typeparam name="TRightId">The strongly-typed identifier for the right side entity.</typeparam>
/// <param name="right">The right side entity instance.</param>
/// <param name="leftId">The identifier of the left side entity.</param>
/// <param name="rightId">The identifier of the right side entity.</param>
public abstract class EntityLink<TRight, TLeftId, TRightId>(TRight right, TLeftId leftId, TRightId rightId) : EntityLink<TLeftId, TRightId>(leftId, rightId)
    where TRight : Entity<TRightId>
    where TLeftId : EntityId<TLeftId>
    where TRightId : EntityId<TRightId>
{
    /// <summary>
    /// Gets or sets the navigation property to the right side entity.
    /// This enables Entity Framework Core to establish proper relationships and support eager loading.
    /// </summary>
    public TRight Right { get; set; } = right;
}

/// <summary>
/// Fully navigable join entity class that includes navigation properties to both sides of the relationship.
/// This is the most feature-complete join entity type, providing access to both related entities
/// for complex business operations and comprehensive query capabilities.
/// </summary>
/// <typeparam name="TLeft">The entity type for the left side of the relationship.</typeparam>
/// <typeparam name="TRight">The entity type for the right side of the relationship.</typeparam>
/// <typeparam name="TLeftId">The strongly-typed identifier for the left side entity.</typeparam>
/// <typeparam name="TRightId">The strongly-typed identifier for the right side entity.</typeparam>
/// <param name="left">The left side entity instance.</param>
/// <param name="right">The right side entity instance.</param>
/// <param name="leftId">The identifier of the left side entity.</param>
/// <param name="rightId">The identifier of the right side entity.</param>
public abstract class EntityLink<TLeft, TRight, TLeftId, TRightId>(TLeft left, TRight right, TLeftId leftId, TRightId rightId) : EntityLink<TRight, TLeftId, TRightId>(right, leftId, rightId)
    where TLeft : Entity<TLeftId>
    where TRight : Entity<TRightId>
    where TLeftId : EntityId<TLeftId>
    where TRightId : EntityId<TRightId>
{
    /// <summary>
    /// Gets or sets the navigation property to the left side entity.
    /// This enables bidirectional navigation and comprehensive relationship access.
    /// </summary>
    public TLeft Left { get; set; } = left;
}

/// <summary>
/// Simplified link class for relationships where one side uses a strongly-typed identifier
/// and the other side uses a value object or primitive type. This pattern is used for
/// relationships with team references and other polymorphic value objects.
/// </summary>
/// <typeparam name="TLeftId">The strongly-typed identifier for the left side entity.</typeparam>
/// <typeparam name="TRight">The type of the right side value (often a value object or primitive).</typeparam>
/// <param name="leftId">The identifier of the left side entity.</param>
/// <param name="right">The right side value or object.</param>
public abstract class Link<TLeftId, TRight>(TLeftId leftId, TRight right)
    where TLeftId : EntityId<TLeftId>
{
    /// <summary>
    /// Gets or sets the identifier of the left side entity in the relationship.
    /// </summary>
    public TLeftId LeftId { get; set; } = leftId;

    /// <summary>
    /// Gets or sets the right side value or object in the relationship.
    /// This can be a value object, primitive type, or other non-entity data.
    /// </summary>
    public TRight Right { get; set; } = right;
}
