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
    /// Loads a tracked stage with the full graph for command paths.
    /// </summary>
    /// <param name="id">The stage identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tracked stage, or <see langword="null"/>.</returns>
    Task<Stage?> GetByIdForUpdateAsync(StageId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a stage without change tracking using the requested graph profile.
    /// </summary>
    /// <param name="id">The stage identity.</param>
    /// <param name="profile">Graph depth to include.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The stage snapshot, or <see langword="null"/>.</returns>
    Task<Stage?> GetByIdReadOnlyAsync(
        StageId id,
        StageLoadProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple stages in one query without change tracking.
    /// </summary>
    /// <param name="ids">Stage identities in competition order.</param>
    /// <param name="profile">Graph depth to include.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Stages in the same order as <paramref name="ids"/>.</returns>
    Task<IReadOnlyList<Stage>> GetByIdsReadOnlyAsync(
        IReadOnlyList<StageId> ids,
        StageLoadProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects stage summary fields for competition detail reads.
    /// </summary>
    /// <param name="ids">Stage identities in competition order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Summaries in the same order as <paramref name="ids"/>.</returns>
    Task<IReadOnlyList<StageSummaryRow>> ListSummariesReadOnlyAsync(
        IReadOnlyList<StageId> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new stage to the current unit of work.
    /// </summary>
    /// <param name="stage">The stage to add.</param>
    void Add(Stage stage);
}
