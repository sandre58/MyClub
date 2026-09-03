// -----------------------------------------------------------------------
// <copyright file="MatchSummaryRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Slim read projection for match list endpoints (no sheet owned collections).
/// </summary>
/// <param name="Id">Match identity.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="HomeEntryId">Home entry.</param>
/// <param name="AwayEntryId">Away entry.</param>
/// <param name="Result">Result when finished; otherwise <see langword="null"/>.</param>
public sealed record MatchSummaryRow(
    MatchId Id,
    StageId StageId,
    MatchStatus Status,
    EntryId HomeEntryId,
    EntryId AwayEntryId,
    MatchResult? Result);
