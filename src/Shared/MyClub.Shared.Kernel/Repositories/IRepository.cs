// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Kernel.Repositories;

/// <summary>
/// Interface for repositories that support both read and write operations on domain entities.
/// Extends IReadOnlyRepository to provide full CRUD (Create, Read, Update, Delete) capabilities.
/// This interface follows the Repository pattern from Domain-Driven Design.
/// </summary>
/// <typeparam name="TEntity">The type of entity managed by this repository, which must inherit from Entity&lt;TId&gt;.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
public interface IRepository<TEntity, in TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    /// <summary>
    /// Adds a new entity to the repository asynchronously.
    /// The entity will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository asynchronously in a single operation.
    /// All entities will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous add range operation.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository asynchronously.
    /// The changes will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities in the repository asynchronously in a single operation.
    /// All changes will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update range operation.</returns>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// The deletion will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities by their identifiers asynchronously in a single operation.
    /// All deletions will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="ids">The collection of identifiers for the entities to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete range operation.</returns>
    Task<int> DeleteRangeAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default);
}
