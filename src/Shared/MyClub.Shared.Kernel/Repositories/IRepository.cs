// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
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
    /// Adds a new entity to the repository.
    /// The entity will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    void Add(TEntity entity);

    /// <summary>
    /// Adds multiple entities to the repository in a single operation.
    /// All entities will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    void AddRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// The changes will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities in the repository in a single operation.
    /// All changes will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    void Update(IEnumerable<TEntity> entities);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// The deletion will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <returns>The number of entities deleted (typically 1 or 0).</returns>
    int Delete(TId id);

    /// <summary>
    /// Deletes multiple entities by their identifiers in a single operation.
    /// All deletions will be persisted when the unit of work is committed.
    /// </summary>
    /// <param name="ids">The collection of identifiers for the entities to delete.</param>
    /// <returns>The number of entities deleted.</returns>
    int DeleteRange(IEnumerable<TId> ids);
}
