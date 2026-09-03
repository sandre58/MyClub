// -----------------------------------------------------------------------
// <copyright file="MatchRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for the Match aggregate.
/// </summary>
internal sealed class MatchRepository(PlayUpDbContext context) : IMatchRepository
{
    /// <inheritdoc />
    public Task<Match?> GetByIdForUpdateAsync(MatchId id, CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, trackChanges: true, cancellationToken);

    /// <inheritdoc />
    public Task<Match?> GetByIdReadOnlyAsync(MatchId id, CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, trackChanges: false, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Match>> ListByStageForUpdateAsync(
        StageId stageId,
        CancellationToken cancellationToken = default) =>
        ListByStageAsync(stageId, trackChanges: true, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<StageId, IReadOnlyList<Match>>> ListByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default)
    {
        if (stageIds.Count == 0)
        {
            return new Dictionary<StageId, IReadOnlyList<Match>>();
        }

        var matches = await context.Set<Match>()
            .AsNoTracking()
            .Where(candidate => stageIds.Contains(candidate.StageId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var match in matches)
        {
            HydrateOrderedCollections(match);
        }

        return matches
            .GroupBy(candidate => candidate.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Match>)[.. group.OrderBy(candidate => candidate.Id.Value)]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchSummaryRow>> ListSummaryRowsByStageReadOnlyAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Set<Match>()
            .AsNoTracking()
            .Where(candidate => candidate.StageId == stageId)
            .Select(candidate => new MatchSummaryRow(
                candidate.Id,
                candidate.StageId,
                candidate.Status,
                candidate.HomeEntryId,
                candidate.AwayEntryId,
                candidate.Result))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.OrderBy(candidate => candidate.Id.Value)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>>> ListAttentionSlicesByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default)
    {
        if (stageIds.Count == 0)
        {
            return new Dictionary<StageId, IReadOnlyList<MatchAttentionSlice>>();
        }

        var rows = await context.Set<Match>()
            .AsNoTracking()
            .Where(candidate => stageIds.Contains(candidate.StageId))
            .Select(candidate => new MatchAttentionSlice(
                candidate.Id,
                candidate.StageId,
                candidate.Status,
                candidate.HomeEntryId,
                candidate.AwayEntryId,
                candidate.Result))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(candidate => candidate.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MatchAttentionSlice>)[.. group.OrderBy(candidate => candidate.Id.Value)]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>> ListSummaryRowsByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default)
    {
        if (stageIds.Count == 0)
        {
            return new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        }

        var rows = await context.Set<Match>()
            .AsNoTracking()
            .Where(candidate => stageIds.Contains(candidate.StageId))
            .Select(candidate => new MatchSummaryRow(
                candidate.Id,
                candidate.StageId,
                candidate.Status,
                candidate.HomeEntryId,
                candidate.AwayEntryId,
                candidate.Result))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(candidate => candidate.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MatchSummaryRow>)[.. group.OrderBy(candidate => candidate.Id.Value)]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchSheetMemberRef>> ListSheetMemberRefsByCompetitionReadOnlyAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var refs = await context.Set<Match>()
            .AsNoTracking()
            .Where(candidate => candidate.CompetitionId == competitionId)
            .SelectMany(
                candidate => candidate.DeclaredParticipations,
                (candidate, participation) => new MatchSheetMemberRef(
                    participation.Side == Side.Home ? candidate.HomeEntryId : candidate.AwayEntryId,
                    participation.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return refs;
    }

    /// <inheritdoc />
    public void Add(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        context.Set<Match>().Add(match);
    }

    private async Task<Match?> LoadByIdAsync(
        MatchId id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = context.Set<Match>().AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        var match = await query
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (match is null)
        {
            return null;
        }

        HydrateOrderedCollections(match);
        return match;
    }

    private async Task<IReadOnlyList<Match>> ListByStageAsync(
        StageId stageId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = context.Set<Match>().AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        var matches = await query
            .Where(candidate => candidate.StageId == stageId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var match in matches)
        {
            HydrateOrderedCollections(match);
        }

        return [.. matches.OrderBy(candidate => candidate.Id.Value)];
    }

    private void HydrateOrderedCollections(Match match)
    {
        MatchDeclaredParticipationsAccessor.Hydrate(
            match,
            match.DeclaredParticipations
                .OrderBy(participation => context.Entry(participation).Property<int>("SortOrder").CurrentValue));

        MatchRecordedGoalsAccessor.Hydrate(
            match,
            match.RecordedGoals
                .OrderBy(goal => context.Entry(goal).Property<int>("SortOrder").CurrentValue));

        MatchRecordedSubstitutionsAccessor.Hydrate(
            match,
            match.RecordedSubstitutions
                .OrderBy(substitution => context.Entry(substitution).Property<int>("SortOrder").CurrentValue));

        MatchRecordedDisciplinaryEventsAccessor.Hydrate(
            match,
            match.RecordedDisciplinaryEvents
                .OrderBy(evt => context.Entry(evt).Property<int>("SortOrder").CurrentValue));
    }
}
