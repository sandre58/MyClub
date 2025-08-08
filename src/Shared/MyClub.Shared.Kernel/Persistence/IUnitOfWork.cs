// -----------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Shared.Kernel.Persistence;

/// <summary>
/// Interface for the Unit of Work pattern implementation.
/// Provides transaction management and coordination for repository operations.
/// The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates writing out changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes within the current unit of work.
    /// This method saves all modifications to the underlying data store and dispatches domain events.
    /// If any operation fails, the entire transaction should be rolled back.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all pending changes within the current unit of work.
    /// This method discards all modifications that have not yet been committed to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
