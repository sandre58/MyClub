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
    public Task<Match?> GetByIdAsync(MatchId id, CancellationToken cancellationToken = default) =>
        context.Set<Match>().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Match>> ListByStageAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        // Filter in SQL; order by MatchId.Value in memory (typed Id is not IComparable for providers).
        var matches = await context.Set<Match>()
            .Where(candidate => candidate.StageId == stageId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. matches.OrderBy(candidate => candidate.Id.Value)];
    }

    /// <inheritdoc />
    public void Add(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        context.Set<Match>().Add(match);
    }
}
