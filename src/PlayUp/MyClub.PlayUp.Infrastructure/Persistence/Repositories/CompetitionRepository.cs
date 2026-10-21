// -----------------------------------------------------------------------
// <copyright file="CompetitionRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for the Competition aggregate.
/// </summary>
internal sealed class CompetitionRepository(PlayUpDbContext context) : ICompetitionRepository
{
    /// <inheritdoc />
    public Task<Competition?> GetByIdForUpdateAsync(CompetitionId id, CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, trackChanges: true, cancellationToken);

    /// <inheritdoc />
    public Task<Competition?> GetByIdReadOnlyAsync(CompetitionId id, CancellationToken cancellationToken = default) =>
        LoadByIdAsync(id, trackChanges: false, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competition>> ListAsync(CancellationToken cancellationToken = default)
    {
        // List does not hydrate StageIds / entry sort — enough for Competition List rows.
        var competitions = await context.Set<Competition>()
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. competitions
            .OrderBy(competition => competition.Name.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(competition => competition.Id.Value)];
    }

    /// <inheritdoc />
    public void Add(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);
        context.Set<Competition>().Add(competition);
        CompetitionStageIdsAccessor.MarkHydrated(competition);
    }

    private async Task<Competition?> LoadByIdAsync(
        CompetitionId id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = context.Set<Competition>().AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        var competition = await query
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (competition is null)
        {
            return null;
        }

        var stageRefQuery = context.Set<CompetitionStageRef>().AsQueryable();
        if (!trackChanges)
        {
            stageRefQuery = stageRefQuery.AsNoTracking();
        }

        var rows = await stageRefQuery
            .Where(row => row.CompetitionId == id)
            .OrderBy(row => row.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        CompetitionStageIdsAccessor.Hydrate(competition, rows.Select(row => row.StageId));

        var orderedEntries = competition.Entries
            .OrderBy(entry => context.Entry(entry).Property<int>("SortOrder").CurrentValue)
            .ToList();
        CompetitionEntriesAccessor.Hydrate(competition, orderedEntries);

        foreach (var entry in competition.Entries)
        {
            var orderedMembers = entry.DeclaredMembers
                .OrderBy(member => context.Entry(member).Property<int>("SortOrder").CurrentValue)
                .ToList();
            CompetitionDeclaredMembersAccessor.Hydrate(entry, orderedMembers);
        }

        return competition;
    }
}
