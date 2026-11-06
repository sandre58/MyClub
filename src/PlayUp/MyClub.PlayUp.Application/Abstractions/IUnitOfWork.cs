// -----------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Commits the current persistence unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="write"/> inside one transaction that excludes another writer
    /// for the same competition. The delegate calls <see cref="SaveChangesAsync"/> once.
    /// </summary>
    /// <param name="competitionId">Competition whose cascade confirmation is serialized.</param>
    /// <param name="write">Load, mutate, and save.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the transaction is committed.</returns>
    Task ExecuteExclusiveAsync(
        CompetitionId competitionId,
        Func<CancellationToken, Task> write,
        CancellationToken cancellationToken = default);
}
