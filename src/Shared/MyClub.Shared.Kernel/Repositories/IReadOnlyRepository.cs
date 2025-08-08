// -----------------------------------------------------------------------
// <copyright file="IReadOnlyRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Kernel.Repositories;

/// <summary>
/// Interface for read-only access to domain entities.
/// Provides query operations without allowing modifications to the underlying data store.
/// This interface is useful for query-only scenarios and follows the CQRS pattern's query side.
/// </summary>
/// <typeparam name="TEntity">The type of entity managed by this repository, which must inherit from Entity&lt;TId&gt;.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
public interface IReadOnlyRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    TEntity? GetById(TId id);

    /// <summary>
    /// Retrieves all entities, optionally filtered by a predicate.
    /// </summary>
    /// <param name="predicate">An optional filter predicate to apply to the entities. If null, all entities are returned.</param>
    /// <returns>A read-only list of entities matching the specified criteria.</returns>
    IReadOnlyList<TEntity> GetAll(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">The unique identifier to check for existence.</param>
    /// <returns>true if an entity with the specified identifier exists; otherwise, false.</returns>
    bool Exists(TId id);

    /// <summary>
    /// Gets the number of entities, optionally filtered by a predicate.
    /// </summary>
    /// <param name="predicate">An optional filter predicate to apply to the entities. If null, counts all entities.</param>
    /// <returns>The number of entities matching the specified criteria.</returns>
    int Count(Expression<Func<TEntity, bool>>? predicate = null);
}
