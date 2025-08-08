// -----------------------------------------------------------------------
// <copyright file="EntityId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Primitives;

/// <summary>
/// Static factory methods for creating strongly-typed entity identifiers.
/// Provides convenient methods to create new instances and convert between different ID representations.
/// </summary>
public static class EntityId
{
    /// <summary>
    /// Creates an entity ID from a string representation.
    /// The string must be a valid GUID format.
    /// </summary>
    /// <typeparam name="TEntityId">The type of entity ID to create.</typeparam>
    /// <param name="value">The string representation of the GUID.</param>
    /// <returns>A new instance of the specified entity ID type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the string cannot be parsed as a valid GUID.</exception>
    public static TEntityId From<TEntityId>(string value)
        where TEntityId : EntityId<TEntityId> => Guid.TryParse(value, out var guid)
            ? From<TEntityId>(guid)
            : throw new InvalidOperationException($"Cannot convert '{value}' to {typeof(TEntityId).Name}");

    /// <summary>
    /// Creates an entity ID from a GUID value.
    /// </summary>
    /// <typeparam name="TEntityId">The type of entity ID to create.</typeparam>
    /// <param name="id">The GUID value for the entity ID.</param>
    /// <returns>A new instance of the specified entity ID type.</returns>
    public static TEntityId From<TEntityId>(Guid id)
        where TEntityId : EntityId<TEntityId> => (TEntityId)Activator.CreateInstance(typeof(TEntityId), id)!;

    /// <summary>
    /// Creates a new entity ID with a randomly generated GUID.
    /// </summary>
    /// <typeparam name="TEntityId">The type of entity ID to create.</typeparam>
    /// <returns>A new instance of the specified entity ID type with a new GUID.</returns>
    public static TEntityId New<TEntityId>()
        where TEntityId : EntityId<TEntityId> => From<TEntityId>(Guid.NewGuid());
}

/// <summary>
/// Base record for strongly-typed entity identifiers.
/// Provides type-safe entity IDs that prevent mixing different entity types accidentally.
/// This is a key pattern in Domain-Driven Design for maintaining type safety with entity identifiers.
/// </summary>
/// <typeparam name="TSelf">The specific type inheriting from this base class (self-referencing generic pattern).</typeparam>
/// <param name="Value">The underlying GUID value for this entity ID.</param>
public abstract record EntityId<TSelf>(Guid Value) : IEquatable<TSelf>
    where TSelf : EntityId<TSelf>
{
    /// <summary>
    /// Creates an entity ID from a GUID value.
    /// </summary>
    /// <param name="id">The GUID value for the entity ID.</param>
    /// <returns>A new instance of this entity ID type.</returns>
    public static TSelf From(Guid id) => EntityId.From<TSelf>(id);

    /// <summary>
    /// Creates a new entity ID with a randomly generated GUID.
    /// </summary>
    /// <returns>A new instance of this entity ID type with a new GUID.</returns>
    public static TSelf New() => EntityId.New<TSelf>();

    /// <summary>
    /// Gets an empty instance of this entity ID type (with Guid.Empty as the value).
    /// </summary>
    public static readonly TSelf Empty = (TSelf)Activator.CreateInstance(typeof(TSelf), Guid.Empty)!;

    /// <summary>
    /// Determines whether the current entity ID is equal to another entity ID of the same type.
    /// </summary>
    /// <param name="other">The entity ID to compare with this entity ID.</param>
    /// <returns>true if the entity IDs are equal; otherwise, false.</returns>
    public bool Equals(TSelf? other) => Value.Equals(other?.Value);

    /// <summary>
    /// Returns the hash code for this entity ID based on its GUID value.
    /// </summary>
    /// <returns>A hash code for the current entity ID.</returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns the string representation of the underlying GUID value.
    /// </summary>
    /// <returns>A string representation of the GUID value.</returns>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Implicitly converts an entity ID to its underlying GUID value.
    /// </summary>
    /// <param name="entityId">The entity ID to convert.</param>
    /// <returns>The underlying GUID value.</returns>
    public static implicit operator Guid(EntityId<TSelf> entityId) => ToGuid(entityId);

    /// <summary>
    /// Implicitly converts a GUID to an entity ID of the appropriate type.
    /// </summary>
    /// <param name="guid">The GUID to convert.</param>
    /// <returns>An entity ID wrapping the specified GUID.</returns>
    public static implicit operator EntityId<TSelf>(Guid guid) => ToEntityId(guid);

    /// <summary>
    /// Converts a GUID to an entity ID of the appropriate type.
    /// </summary>
    /// <param name="guid">The GUID to convert.</param>
    /// <returns>An entity ID wrapping the specified GUID.</returns>
    public static EntityId<TSelf> ToEntityId(Guid guid) => From(guid);

    /// <summary>
    /// Converts an entity ID to its underlying GUID value.
    /// </summary>
    /// <param name="entityId">The entity ID to convert.</param>
    /// <returns>The underlying GUID value.</returns>
    public static Guid ToGuid(EntityId<TSelf> entityId) => entityId.Value;
}
