// -----------------------------------------------------------------------
// <copyright file="PlayUpDatabaseReset.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL reset via EnsureDeleted + Migrate (callers must enforce workspace safety).
/// </summary>
public sealed class PlayUpDatabaseReset(PlayUpDbContext dbContext) : IPlayUpDatabaseReset
{
    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
