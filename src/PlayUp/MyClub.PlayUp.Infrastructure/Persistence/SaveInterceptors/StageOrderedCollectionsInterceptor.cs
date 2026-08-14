// -----------------------------------------------------------------------
// <copyright file="StageOrderedCollectionsInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.SaveInterceptors;

/// <summary>
/// Writes Domain list order into shadow <c>sort_order</c>, and syncs group_entries / fixture_attachments rows.
/// Domain remains the source of truth; Slots / DirectAssignments / MatchPlacements have natural keys and need no sort_order sync.
/// Draw/Penalty sort_order sync is independent of Groups/Rounds/Matchdays/Fixtures (shared helper only).
/// </summary>
internal sealed class StageOrderedCollectionsInterceptor : SaveChangesInterceptor
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

        var stages = context.ChangeTracker.Entries<Stage>()
            .Where(entry => entry.State is not (EntityState.Deleted or EntityState.Detached))
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var stage in stages)
        {
            SyncSortOrder(context, StageOrderedCollectionsAccessor.GetGroups(stage));
            SyncSortOrder(context, StageOrderedCollectionsAccessor.GetRounds(stage));
            SyncSortOrder(context, StageOrderedCollectionsAccessor.GetMatchdays(stage));
            SyncSortOrder(context, StageOrderedCollectionsAccessor.GetDraws(stage));
            SyncSortOrder(context, StageOrderedCollectionsAccessor.GetPenalties(stage));

            foreach (var group in stage.Groups)
            {
                SyncGroupEntries(context, group);
            }

            foreach (var round in stage.Rounds)
            {
                SyncSortOrder(context, StageOrderedCollectionsAccessor.GetRoundFixtures(round));
            }

            foreach (var matchday in stage.Matchdays)
            {
                SyncSortOrder(context, StageOrderedCollectionsAccessor.GetMatchdayFixtures(matchday));
            }

            foreach (var fixture in EnumerateFixtures(stage))
            {
                SyncAttachments(context, fixture);
            }
        }

        context.ChangeTracker.DetectChanges();
    }

    private static IEnumerable<Fixture> EnumerateFixtures(Stage stage) =>
        StageOrderedCollectionsAccessor.GetRounds(stage)
            .SelectMany(StageOrderedCollectionsAccessor.GetRoundFixtures)
            .Concat(
                StageOrderedCollectionsAccessor.GetMatchdays(stage)
                    .SelectMany(StageOrderedCollectionsAccessor.GetMatchdayFixtures));

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

    private static void SyncGroupEntries(DbContext context, Group group)
    {
        var groupEntry = context.Entry(group);
        if (groupEntry.State is EntityState.Detached or EntityState.Deleted)
        {
            return;
        }

        var desired = StageOrderedCollectionsAccessor.GetEntryIds(group);
        var existing = LoadExistingGroupEntries(context, groupEntry, group);

        foreach (var row in existing.Where(row => !desired.Contains(row.EntryId)))
        {
            context.Remove(row);
        }

        for (var index = 0; index < desired.Count; index++)
        {
            var entryId = desired[index];
            var row = existing.Find(candidate => candidate.EntryId == entryId);
            if (row is null)
            {
                context.Add(new GroupEntryRef
                {
                    GroupId = group.Id,
                    EntryId = entryId,
                    SortOrder = index
                });
            }
            else if (row.SortOrder != index)
            {
                row.SortOrder = index;
            }
        }
    }

    private static List<GroupEntryRef> LoadExistingGroupEntries(
        DbContext context,
        EntityEntry<Group> groupEntry,
        Group group)
    {
        var local = context.Set<GroupEntryRef>().Local
            .Where(row => row.GroupId == group.Id)
            .ToList();

        return groupEntry.State == EntityState.Added
            ? local
            : [.. context.Set<GroupEntryRef>().Where(row => row.GroupId == group.Id)];
    }

    private static void SyncAttachments(DbContext context, Fixture fixture)
    {
        var fixtureEntry = context.Entry(fixture);
        if (fixtureEntry.State is EntityState.Detached or EntityState.Deleted)
        {
            return;
        }

        var desired = StageOrderedCollectionsAccessor.GetAttachments(fixture);
        var existing = LoadExistingAttachments(context, fixtureEntry, fixture);

        foreach (var row in existing.Where(row => desired.All(attachment => attachment.LegIndex != row.LegIndex)))
        {
            context.Remove(row);
        }

        foreach (var attachment in desired)
        {
            var row = existing.Find(candidate => candidate.LegIndex == attachment.LegIndex);
            if (row is null)
            {
                context.Add(new FixtureAttachmentRef
                {
                    FixtureId = fixture.Id,
                    LegIndex = attachment.LegIndex,
                    MatchId = attachment.MatchId
                });
            }
            else if (!row.MatchId.Equals(attachment.MatchId))
            {
                row.MatchId = attachment.MatchId;
            }
        }
    }

    private static List<FixtureAttachmentRef> LoadExistingAttachments(
        DbContext context,
        EntityEntry<Fixture> fixtureEntry,
        Fixture fixture)
    {
        var local = context.Set<FixtureAttachmentRef>().Local
            .Where(row => row.FixtureId == fixture.Id)
            .ToList();

        return fixtureEntry.State == EntityState.Added
            ? local
            : [.. context.Set<FixtureAttachmentRef>().Where(row => row.FixtureId == fixture.Id)];
    }
}
