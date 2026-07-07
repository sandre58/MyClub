// -----------------------------------------------------------------------
// <copyright file="Repository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Shared.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation using Entity Framework Core.
/// Provides common CRUD operations for domain entities.
/// Inherits read operations from ReadOnlyRepositoryBase and adds write operations.
/// Generic implementation that can be used across different modules with their own DbContext.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TId">The entity's strongly typed identifier.</typeparam>
/// <typeparam name="TDbContext">The DbContext type for the specific module.</typeparam>
public abstract class Repository<TEntity, TId, TDbContext>(TDbContext context) : ReadOnlyRepository<TEntity, TId, TDbContext>(context), IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TDbContext : DbContext
{
    #region Write Operations

    /// <summary>
    /// Adds a new entity to the repository asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        OnAdding(entity);
        await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds multiple entities to the repository asynchronously.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        foreach (var entity in entityList)
            OnAdding(entity);
        await DbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an existing entity in the repository asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        OnUpdating(entity);
        DbSet.Update(entity);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Updates multiple entities in the repository asynchronously.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        foreach (var entity in entityList)
            OnUpdating(entity);
        DbSet.UpdateRange(entityList);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    /// <returns>The number of entities deleted (0 or 1).</returns>
    public virtual async Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return 0;
        OnDeleting(entity);
        DbSet.Remove(entity);
        return 1;
    }

    /// <summary>
    /// Deletes multiple entities by their identifiers asynchronously.
    /// </summary>
    /// <param name="ids">The identifiers of the entities to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation (optional).</param>
    /// <returns>The number of entities deleted.</returns>
    public virtual async Task<int> DeleteRangeAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var idsList = ids.ToList();
        if (idsList.Count == 0)
            return 0;
        var entities = await DbSet.Where(e => idsList.Contains(e.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0)
            return 0;
        foreach (var entity in entities)
            OnDeleting(entity);
        DbSet.RemoveRange(entities);
        return entities.Count;
    }

    #endregion

    #region Protected Virtual Methods for Customization

    /// <summary>
    /// Applies any additional logic before adding an entity.
    /// Override this method in derived repositories for custom validation or preparation.
    /// </summary>
    /// <param name="entity">The entity being added.</param>
    protected virtual void OnAdding(TEntity entity) { }

    /// <summary>
    /// Applies any additional logic before updating an entity.
    /// Override this method in derived repositories for custom validation or preparation.
    /// </summary>
    /// <param name="entity">The entity being updated.</param>
    protected virtual void OnUpdating(TEntity entity) { }

    /// <summary>
    /// Applies any additional logic before deleting an entity.
    /// Override this method in derived repositories for custom validation or cleanup.
    /// </summary>
    /// <param name="entity">The entity being deleted.</param>
    protected virtual void OnDeleting(TEntity entity) { }

    #endregion
}
