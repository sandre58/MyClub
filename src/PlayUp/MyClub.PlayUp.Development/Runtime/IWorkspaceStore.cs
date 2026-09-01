// -----------------------------------------------------------------------
// <copyright file="IWorkspaceStore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Clears the Development Workspace persistence backend before seeding.
/// </summary>
public interface IWorkspaceStore
{
    /// <summary>
    /// Resets all Play'Up data for the active persistence mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the store is empty.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
