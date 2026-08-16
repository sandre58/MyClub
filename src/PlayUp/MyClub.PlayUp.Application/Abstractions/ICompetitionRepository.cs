// -----------------------------------------------------------------------
// <copyright file="ICompetitionRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Persistence port for the Competition aggregate.
/// </summary>
public interface ICompetitionRepository
{
    /// <summary>
    /// Loads a competition by identity, or <see langword="null"/> if it does not exist.
    /// </summary>
    /// <param name="id">The competition identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tracked competition, or <see langword="null"/>.</returns>
    Task<Competition?> GetByIdAsync(CompetitionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists competitions for the organizer Competition List (no stage hydration).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Competitions ordered by name, then identity.</returns>
    Task<IReadOnlyList<Competition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new competition to the current unit of work.
    /// </summary>
    /// <param name="competition">The competition to add.</param>
    void Add(Competition competition);
}
