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
    /// Loads a tracked match for command paths that mutate the aggregate.
    /// </summary>
    /// <param name="id">The match identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tracked match, or <see langword="null"/>.</returns>
    Task<Match?> GetByIdForUpdateAsync(MatchId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a match without change tracking for read-only queries.
    /// </summary>
    /// <param name="id">The match identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The match snapshot, or <see langword="null"/>.</returns>
    Task<Match?> GetByIdReadOnlyAsync(MatchId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists tracked matches belonging to a stage (command paths).
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Matches for the stage (possibly empty).</returns>
    Task<IReadOnlyList<Match>> ListByStageForUpdateAsync(
        StageId stageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists matches for multiple stages in one query without change tracking.
    /// </summary>
    /// <param name="stageIds">Stage identities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Matches grouped by stage (empty stages omitted).</returns>
    Task<IReadOnlyDictionary<StageId, IReadOnlyList<Match>>> ListByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects match summary rows for a stage list endpoint (no sheet collections).
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Summary rows ordered by MatchId.</returns>
    Task<IReadOnlyList<MatchSummaryRow>> ListSummaryRowsByStageReadOnlyAsync(
        StageId stageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects match summary rows for multiple stages in one query.
    /// </summary>
    /// <param name="stageIds">Stage identities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Rows grouped by stage (empty stages omitted).</returns>
    Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>> ListSummaryRowsByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects attention slices for multiple stages in one query (no sheet collections).
    /// </summary>
    /// <param name="stageIds">Stage identities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Slices grouped by stage (empty stages omitted).</returns>
    Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>>> ListAttentionSlicesByStageIdsReadOnlyAsync(
        IReadOnlyList<StageId> stageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects declared composition sheet members for a competition (no match owned collections).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Entry/member pairs referenced on a match sheet.</returns>
    Task<IReadOnlyList<MatchSheetMemberRef>> ListSheetMemberRefsByCompetitionReadOnlyAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new match to the current unit of work.
    /// </summary>
    /// <param name="match">The match to add.</param>
    void Add(Match match);

    /// <summary>
    /// Removes a match from the current unit of work (after fixture detach / placement clear).
    /// </summary>
    /// <param name="match">The match to remove.</param>
    void Remove(Match match);
}
