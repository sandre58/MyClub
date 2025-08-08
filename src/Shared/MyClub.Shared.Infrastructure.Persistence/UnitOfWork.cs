// -----------------------------------------------------------------------
// <copyright file="UnitOfWork.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClub.Shared.Kernel.Events;
using MyClub.Shared.Kernel.Persistence;

namespace MyClub.Shared.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation using Entity Framework Core.
/// Manages database transactions and coordinates changes across multiple repositories.
/// Ensures atomicity of operations and proper transaction management.
/// Collects and dispatches domain events before committing changes.
/// </summary>
public abstract class UnitOfWork<TDbContext>(TDbContext context, IDomainEventDispatcher? domainEventDispatcher = null) : IUnitOfWork, IDisposable
    where TDbContext : DbContext
{
    private bool _isDisposed;

    protected TDbContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Commits all pending changes to the database within a transaction.
    /// Saves all entity changes tracked by the DbContext.
    /// Collects and dispatches domain events before committing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Collect domain events before saving changes
            var domainEvents = GetDomainEvents();

            // Save changes to database
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Dispatch domain events after successful commit
            if (domainEventDispatcher != null && domainEvents.Count > 0)
            {
                await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (DbUpdateException ex)
        {
            // Log the exception details for debugging
            // Transform into domain-specific exception if needed
            throw new InvalidOperationException("Failed to commit changes to the database.", ex);
        }
        catch (Exception ex)
        {
            // Handle other potential exceptions
            throw new InvalidOperationException("An unexpected error occurred while committing changes.", ex);
        }
    }

    /// <summary>
    /// Rolls back all pending changes by discarding tracked entities.
    /// Resets the DbContext to its previous state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Discard all changes by reloading entities from a database
            foreach (var entry in Context.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    case EntityState.Unchanged:
                    case EntityState.Detached:
                    default:
                        // No action needed for these states
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while rolling back changes.", ex);
        }
    }

    /// <summary>
    /// Collects all domain events from tracked entities.
    /// </summary>
    /// <returns>A collection of domain events from all entities with events.</returns>
    private List<IDomainEvent> GetDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        var entitiesWithEvents = Context.ChangeTracker.Entries()
            .Where(e => e.Entity is IHasDomainEvents { DomainEvents.Count: > 0 })
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            domainEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            // free managed resources
            Context.Dispose();
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Disposes the Unit of Work and its associated DbContext.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
