// -----------------------------------------------------------------------
// <copyright file="MatchParticipantSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Lightweight participant view of a materialized match for determination-source coherence.
/// </summary>
/// <remarks>
/// Application builds these from loaded <c>Match</c> aggregates. Domain never loads Match/EF here.
/// </remarks>
/// <param name="MatchId">Match identity.</param>
/// <param name="HomeEntryId">Home participant when known.</param>
/// <param name="AwayEntryId">Away participant when known.</param>
public readonly record struct MatchParticipantSnapshot(
    MatchId MatchId,
    EntryId? HomeEntryId,
    EntryId? AwayEntryId);
