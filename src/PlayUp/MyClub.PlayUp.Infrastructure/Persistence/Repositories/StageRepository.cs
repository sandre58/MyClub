// -----------------------------------------------------------------------
// <copyright file="StageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for the Stage aggregate (structure + runtime state).
/// </summary>
internal sealed class StageRepository(PlayUpDbContext context) : IStageRepository
{
    /// <inheritdoc />
    public Task<Stage?> GetByIdForUpdateAsync(StageId id, CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, StageLoadProfile.Full, trackChanges: true, cancellationToken);

    /// <inheritdoc />
    public Task<Stage?> GetByIdReadOnlyAsync(
        StageId id,
        StageLoadProfile profile,
        CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, profile, trackChanges: false, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Stage>> GetByIdsReadOnlyAsync(
        IReadOnlyList<StageId> ids,
        StageLoadProfile profile,
        CancellationToken cancellationToken = default) =>
        GetByIdsReadOnlyAsync(ids, StageReadCapabilities.FromProfile(profile), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Stage>> GetByIdsReadOnlyAsync(
        IReadOnlyList<StageId> ids,
        StageReadCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var query = ApplyReadShape(context.Set<Stage>().AsNoTracking().AsSplitQuery(), capabilities)
            .Where(candidate => ids.Contains(candidate.Id));

        var loaded = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (RequiresStructureHydration(capabilities))
        {
            foreach (var stage in loaded)
            {
                await HydrateOrderedCollectionsAsync(stage, cancellationToken).ConfigureAwait(false);
            }
        }

        var byId = loaded.ToDictionary(stage => stage.Id);
        var ordered = new List<Stage>(ids.Count);
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var stage))
            {
                continue;
            }

            ordered.Add(stage);
        }

        return ordered;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StageSummaryRow>> ListSummariesReadOnlyAsync(
        IReadOnlyList<StageId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await context.Set<Stage>()
            .AsNoTracking()
            .Where(stage => ids.Contains(stage.Id))
            .Select(stage => new StageSummaryRow(stage.Id, stage.Name.Value, stage.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byId = rows.ToDictionary(row => row.Id);
        var ordered = new List<StageSummaryRow>(ids.Count);
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var row))
            {
                continue;
            }

            ordered.Add(row);
        }

        return ordered;
    }

    /// <inheritdoc />
    public void Add(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        context.Set<Stage>().Add(stage);
    }

    private async Task<Stage?> LoadByIdAsync(
        StageId id,
        StageLoadProfile profile,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        if (trackChanges)
        {
            var tracked = context.Set<Stage>().Local.FirstOrDefault(candidate => candidate.Id.Equals(id));
            if (tracked is not null)
            {
                return tracked;
            }
        }

        var root = context.Set<Stage>().AsQueryable();
        if (!trackChanges)
        {
            root = root.AsNoTracking();
        }

        var query = ApplyReadShape(root.AsSplitQuery(), StageReadCapabilities.FromProfile(profile));
        var stage = await query
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stage is null)
        {
            return null;
        }

        if (RequiresStructureHydration(StageReadCapabilities.FromProfile(profile)))
        {
            await HydrateOrderedCollectionsAsync(stage, cancellationToken).ConfigureAwait(false);
        }

        return stage;
    }

    private static bool RequiresStructureHydration(StageReadCapabilities capabilities) =>
        capabilities.Profile >= StageLoadProfile.Structure;

    private static IQueryable<Stage> ApplyReadShape(IQueryable<Stage> query, StageReadCapabilities capabilities)
    {
        if (capabilities.Profile == StageLoadProfile.Full)
        {
            return ApplyProfile(query, StageLoadProfile.Full);
        }

        query = ApplyProfile(query, capabilities.Profile);

        if (capabilities.IncludeDraws)
        {
            query = query.Include(candidate => candidate.Draws);
        }

        if (capabilities.IncludeSlots)
        {
            query = query.Include(candidate => candidate.Slots);
        }

        if (capabilities.IncludePenalties)
        {
            query = query.Include(candidate => candidate.Penalties);
        }

        return query;
    }

    private static IQueryable<Stage> ApplyProfile(IQueryable<Stage> query, StageLoadProfile profile) =>
        profile switch
        {
            StageLoadProfile.Summary => query,
            StageLoadProfile.Structure => query
                .Include(candidate => candidate.Groups)
                .Include(candidate => candidate.Rounds)
                .ThenInclude(round => round.Fixtures)
                .Include(candidate => candidate.Matchdays)
                .ThenInclude(matchday => matchday.Fixtures)
                .Include(candidate => candidate.MatchPlacements),
            StageLoadProfile.Full => query
                .Include(candidate => candidate.Groups)
                .Include(candidate => candidate.Rounds)
                .ThenInclude(round => round.Fixtures)
                .Include(candidate => candidate.Matchdays)
                .ThenInclude(matchday => matchday.Fixtures)
                .Include(candidate => candidate.Slots)
                .Include(candidate => candidate.DirectAssignments)
                .Include(candidate => candidate.Draws)
                .Include(candidate => candidate.Penalties)
                .Include(candidate => candidate.MatchPlacements)
                .Include(candidate => candidate.SwissByeHistory),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

    private async Task HydrateOrderedCollectionsAsync(Stage stage, CancellationToken cancellationToken)
    {
        var groups = StageOrderedCollectionsAccessor.GetGroups(stage)
            .OrderBy(group => context.Entry(group).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderGroups(stage, groups);

        var groupIds = groups.Select(group => group.Id).ToArray();
        var entryRows = groupIds.Length == 0
            ? []
            : await context.Set<GroupEntryRef>()
                .AsNoTracking()
                .Where(row => groupIds.AsEnumerable().Contains(row.GroupId))
                .OrderBy(row => row.SortOrder)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        foreach (var group in groups)
        {
            var orderedEntryIds = entryRows
                .Where(row => row.GroupId == group.Id)
                .OrderBy(row => row.SortOrder)
                .Select(row => row.EntryId);
            StageOrderedCollectionsAccessor.ReorderEntryIds(group, orderedEntryIds);
        }

        var rounds = StageOrderedCollectionsAccessor.GetRounds(stage)
            .OrderBy(round => context.Entry(round).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderRounds(stage, rounds);

        foreach (var round in rounds)
        {
            var fixtures = StageOrderedCollectionsAccessor.GetRoundFixtures(round)
                .OrderBy(fixture => context.Entry(fixture).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderRoundFixtures(round, fixtures);
            await HydrateAttachmentsAsync(fixtures, cancellationToken).ConfigureAwait(false);
        }

        var matchdays = StageOrderedCollectionsAccessor.GetMatchdays(stage)
            .OrderBy(matchday => context.Entry(matchday).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderMatchdays(stage, matchdays);

        foreach (var matchday in matchdays)
        {
            var fixtures = StageOrderedCollectionsAccessor.GetMatchdayFixtures(matchday)
                .OrderBy(fixture => context.Entry(fixture).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderMatchdayFixtures(matchday, fixtures);
            await HydrateAttachmentsAsync(fixtures, cancellationToken).ConfigureAwait(false);
        }

        if (StageOrderedCollectionsAccessor.GetDraws(stage).Count > 0)
        {
            var draws = StageOrderedCollectionsAccessor.GetDraws(stage)
                .OrderBy(draw => context.Entry(draw).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderDraws(stage, draws);
        }

        if (StageOrderedCollectionsAccessor.GetPenalties(stage).Count > 0)
        {
            var penalties = StageOrderedCollectionsAccessor.GetPenalties(stage)
                .OrderBy(penalty => context.Entry(penalty).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderPenalties(stage, penalties);
        }
    }

    private async Task HydrateAttachmentsAsync(List<Fixture> fixtures, CancellationToken cancellationToken)
    {
        if (fixtures.Count == 0)
        {
            return;
        }

        var fixtureIds = fixtures.Select(fixture => fixture.Id).ToArray();
        var rows = await context.Set<FixtureAttachmentRef>()
            .AsNoTracking()
            .Where(row => fixtureIds.AsEnumerable().Contains(row.FixtureId))
            .OrderBy(row => row.LegIndex)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var fixture in fixtures)
        {
            var attachments = rows
                .Where(row => row.FixtureId == fixture.Id)
                .OrderBy(row => row.LegIndex)
                .Select(row => new MatchAttachment(row.MatchId, row.LegIndex));
            StageOrderedCollectionsAccessor.ReplaceAttachments(fixture, attachments);
        }
    }
}
