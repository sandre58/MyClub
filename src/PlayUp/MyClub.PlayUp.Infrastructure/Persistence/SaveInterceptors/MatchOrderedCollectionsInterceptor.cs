// -----------------------------------------------------------------------
// <copyright file="MatchOrderedCollectionsInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence.SaveInterceptors;

/// <summary>
/// Writes Domain collection order into shadow sort_order for Match owned collections.
/// </summary>
internal sealed class MatchOrderedCollectionsInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Sync(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Sync(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void Sync(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var matches = context.ChangeTracker.Entries<Match>()
            .Where(entry => entry.State is not (EntityState.Deleted or EntityState.Detached))
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var match in matches)
        {
            SyncSortOrder(context, match.DeclaredParticipations);
            SyncSortOrder(context, match.RecordedGoals);
            SyncSortOrder(context, match.RecordedSubstitutions);
        }

        context.ChangeTracker.DetectChanges();
    }

    private static void SyncSortOrder<TEntity>(DbContext context, IReadOnlyList<TEntity> items)
        where TEntity : class
    {
        for (var index = 0; index < items.Count; index++)
        {
            var owned = context.Entry(items[index]);
            if (owned.State is EntityState.Detached)
            {
                continue;
            }

            var sortOrder = owned.Property<int>("SortOrder");
            if (!Equals(sortOrder.CurrentValue, index))
            {
                sortOrder.CurrentValue = index;
            }
        }
    }
}
