// -----------------------------------------------------------------------
// <copyright file="IPlayUpDatabaseReset.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Wipes and recreates the Play'Up PostgreSQL schema (Development Workspace only).
/// </summary>
public interface IPlayUpDatabaseReset
{
    /// <summary>
    /// Deletes the database (or schema contents) and re-applies migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the database is empty and migrated.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
