// -----------------------------------------------------------------------
// <copyright file="CompetitionReadBundle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Shared read snapshot for competition-scoped queries (single load, multiple assemblers).
/// </summary>
/// <param name="Competition">Loaded competition.</param>
/// <param name="Stages">Competition stages in canonical order.</param>
/// <param name="MatchesByStage">Projected match rows keyed by stage.</param>
internal sealed record CompetitionReadBundle(
    Competition Competition,
    IReadOnlyList<Stage> Stages,
    IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> MatchesByStage);
