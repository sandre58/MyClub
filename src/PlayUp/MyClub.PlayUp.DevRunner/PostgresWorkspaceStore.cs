// -----------------------------------------------------------------------
// <copyright file="PostgresWorkspaceStore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Infrastructure.Persistence;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Infrastructure.Persistence;

namespace MyClub.PlayUp.DevRunner;

/// <summary>
/// PostgreSQL workspace reset for the DevRunner CLI (Play'up + Media schema + local files).
/// </summary>
internal sealed class PostgresWorkspaceStore(IServiceScopeFactory scopeFactory, string mediaStorageRoot)
    : IWorkspaceStore
{
    /// <summary>
    /// Resets all Play'up data for the active persistence mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the store is empty.</returns>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var reset = scope.ServiceProvider.GetRequiredService<IPlayUpDatabaseReset>();
            await reset.ResetAsync(cancellationToken).ConfigureAwait(false);

            var mediaDb = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await mediaDb.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        ClearMediaStorage(mediaStorageRoot);
    }

    private static void ClearMediaStorage(string root)
    {
        if (!Directory.Exists(root))
        {
            Directory.CreateDirectory(root);
            return;
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(root))
        {
            if (Directory.Exists(entry))
            {
                Directory.Delete(entry, recursive: true);
            }
            else
            {
                File.Delete(entry);
            }
        }
    }
}
