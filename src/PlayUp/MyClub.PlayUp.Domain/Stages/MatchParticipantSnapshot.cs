// -----------------------------------------------------------------------
// <copyright file="MatchParticipantSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Lightweight participant view of a materialized match for determination-source coherence (D4 / F1).
/// </summary>
/// <remarks>
/// Application builds these from loaded <c>Match</c> aggregates. Domain never loads Match/EF here.
/// </remarks>
/// <param name="MatchId">Match identity.</param>
/// <param name="HomeEntryId">Immutable home participant.</param>
/// <param name="AwayEntryId">Immutable away participant.</param>
public readonly record struct MatchParticipantSnapshot(
    MatchId MatchId,
    EntryId HomeEntryId,
    EntryId AwayEntryId);
