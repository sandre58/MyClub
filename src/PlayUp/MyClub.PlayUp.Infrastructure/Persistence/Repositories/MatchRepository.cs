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
    public void Add(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        context.Set<Match>().Add(match);
    }
}
