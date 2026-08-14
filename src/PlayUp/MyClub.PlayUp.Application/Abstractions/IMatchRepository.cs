// -----------------------------------------------------------------------
// <copyright file="IMatchRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Persistence port for the Match aggregate.
/// </summary>
public interface IMatchRepository
{
    /// <summary>
    /// Loads a match by identity, or <see langword="null"/> if it does not exist.
    /// </summary>
    /// <param name="id">The match identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tracked match, or <see langword="null"/>.</returns>
    Task<Match?> GetByIdAsync(MatchId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new match to the current unit of work.
    /// </summary>
    /// <param name="match">The match to add.</param>
    void Add(Match match);
}
