// -----------------------------------------------------------------------
// <copyright file="PostgresWorkspaceStore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Infrastructure.Persistence;

namespace MyClub.PlayUp.DevRunner;

/// <summary>
/// PostgreSQL workspace reset for the DevRunner CLI.
/// </summary>
internal sealed class PostgresWorkspaceStore(IServiceScopeFactory scopeFactory) : IWorkspaceStore
{
    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var reset = scope.ServiceProvider.GetRequiredService<IPlayUpDatabaseReset>();
            await reset.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
