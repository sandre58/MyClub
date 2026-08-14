// -----------------------------------------------------------------------
// <copyright file="IStageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Persistence port for the Stage aggregate.
/// </summary>
public interface IStageRepository
{
    /// <summary>
    /// Loads a stage by identity, or <see langword="null"/> if it does not exist.
    /// </summary>
    /// <param name="id">The stage identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tracked stage, or <see langword="null"/>.</returns>
    Task<Stage?> GetByIdAsync(StageId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new stage to the current unit of work.
    /// </summary>
    /// <param name="stage">The stage to add.</param>
    void Add(Stage stage);
}
