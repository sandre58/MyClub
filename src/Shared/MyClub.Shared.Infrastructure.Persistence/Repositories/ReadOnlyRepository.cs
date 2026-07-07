// -----------------------------------------------------------------------
// <copyright file="ReadOnlyRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Shared.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base read-only repository implementation using Entity Framework Core.
/// Provides common read operations for domain entities.
/// Generic implementation that can be used across different modules with their own DbContext.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TId">The entity's strongly typed identifier.</typeparam>
/// <typeparam name="TDbContext">The DbContext type for the specific module.</typeparam>
public abstract class ReadOnlyRepository<TEntity, TId, TDbContext>(TDbContext context) : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TDbContext : DbContext
{
    protected DbSet<TEntity> DbSet { get; } = context.Set<TEntity>();

    #region Read Operations

    /// <summary>
    /// Gets an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found, otherwise null.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var query = ConfigureQuery(DbSet.AsQueryable());

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken).ConfigureAwait(false);
    }
    /// <summary>
    /// Gets all entities that match the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">Optional filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of matching entities.</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = ConfigureQuery(DbSet.AsQueryable());

        if (predicate is not null)
            query = query.Where(predicate);

        return (await query.ToListAsync(cancellationToken).ConfigureAwait(false)).AsReadOnly();
    }

    /// <summary>
    /// Checks if an entity exists with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the entity exists, otherwise false.</returns>
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Counts entities that match the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">Optional filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of matching entities.</returns>
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (predicate is not null)
            query = query.Where(predicate);

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Protected Virtual Methods for Customization

    /// <summary>
    /// Configures the query with includes, filters, or other customizations.
    /// Override this method in derived repositories to add specific configurations like Include().
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <returns>The configured query.</returns>
    protected virtual IQueryable<TEntity> ConfigureQuery(IQueryable<TEntity> query) => query;

    /// <summary>
    /// Applies any additional logic when retrieving an entity.
    /// Override this method in derived repositories for custom post-processing.
    /// </summary>
    /// <param name="entity">The entity being retrieved.</param>
    /// <returns>The processed entity.</returns>
    protected virtual TEntity? OnRetrieving(TEntity? entity) => entity;

    #endregion
}
