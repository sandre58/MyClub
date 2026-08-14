// -----------------------------------------------------------------------
// <copyright file="CompetitionOrderedCollectionsInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Infrastructure.Persistence.SaveInterceptors;

/// <summary>
/// Aligns competition_stage_refs rows with Domain StageIds, including when Competition stays Unchanged.
/// </summary>
internal sealed class CompetitionOrderedCollectionsInterceptor : SaveChangesInterceptor
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

        var competitions = context.ChangeTracker.Entries<Competition>()
            .Where(entry => entry.State is not (EntityState.Deleted or EntityState.Detached))
            .ToList();

        foreach (var entry in competitions)
        {
            SyncStageIds(context, entry);
            SyncEntrySortOrder(context, entry.Entity);
        }

        context.ChangeTracker.DetectChanges();
    }

    private static void SyncStageIds(DbContext context, EntityEntry<Competition> entry)
    {
        var competition = entry.Entity;

        if (entry.State != EntityState.Added && !CompetitionStageIdsAccessor.IsHydrated(competition))
        {
            throw new InvalidOperationException(
                "Competition StageIds were not hydrated. Load via ICompetitionRepository.GetByIdAsync (or Add) before saving.");
        }

        var desired = CompetitionStageIdsAccessor.GetList(competition);
        var existing = LoadExistingRows(context, entry, competition);

        foreach (var row in existing.Where(row => !desired.Contains(row.StageId)))
        {
            context.Remove(row);
        }

        for (var index = 0; index < desired.Count; index++)
        {
            var stageId = desired[index];
            var row = existing.Find(candidate => candidate.StageId == stageId);
            if (row is null)
            {
                context.Add(new CompetitionStageRef
                {
                    CompetitionId = competition.Id,
                    StageId = stageId,
                    SortOrder = index
                });
            }
            else
            {
                row.SortOrder = index;
            }
        }
    }

    private static List<CompetitionStageRef> LoadExistingRows(
        DbContext context,
        EntityEntry<Competition> entry,
        Competition competition)
    {
        var local = context.Set<CompetitionStageRef>().Local
            .Where(row => row.CompetitionId == competition.Id)
            .ToList();

        return entry.State == EntityState.Added ? local : [.. context.Set<CompetitionStageRef>().Where(row => row.CompetitionId == competition.Id)];
    }

    private static void SyncEntrySortOrder(DbContext context, Competition competition)
    {
        var entries = competition.Entries;
        for (var index = 0; index < entries.Count; index++)
        {
            var owned = context.Entry(entries[index]);
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
