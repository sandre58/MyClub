// -----------------------------------------------------------------------
// <copyright file="IReadOnlyRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
    /// Retrieves an entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities asynchronously, optionally filtered by a predicate.
    /// </summary>
    /// <param name="predicate">An optional filter predicate to apply to the entities. If null, all entities are returned.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of entities matching the specified criteria.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an entity with the specified identifier exists asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier to check for existence.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if an entity with the specified identifier exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of entities asynchronously, optionally filtered by a predicate.
    /// </summary>
    /// <param name="predicate">An optional filter predicate to apply to the entities. If null, counts all entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of entities matching the specified criteria.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
