// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome of <see cref="MaterializeCupFromOccupiedSlots"/>.
/// </summary>
/// <param name="CreatedMatches">Newly created matches.</param>
/// <param name="AttachedMatchIds">All match ids attached to materialized confrontations.</param>
/// <param name="AlreadyComplete">True when every requested pair was already complete.</param>
public sealed record MaterializeCupFromOccupiedSlotsResult(
    IReadOnlyList<Match> CreatedMatches,
    IReadOnlyList<MatchId> AttachedMatchIds,
    bool AlreadyComplete);
