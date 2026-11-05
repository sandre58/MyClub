// -----------------------------------------------------------------------
// <copyright file="StageDrawBracketLaunchFactsDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Bracket place facts for Tirage launch preflight (Cup slot / Groups draw).
/// </summary>
/// <param name="EntryPlaceCount">Places in launch scope.</param>
/// <param name="OccupiedByEntryCount">Places with an occupant that is not a structural feed: composition occupants and withdrawn occupants. Neither is vacant.</param>
/// <param name="ReservedByStructuralFeedCount">Places with a unique structural feed, including a unique direct feed with no occupant.</param>
/// <param name="FeedResolutionInvalidCount">Places with ambiguous or invalid feed resolution.</param>
/// <param name="BlockingPlaceCount">Occupied + reserved + invalid — blocks Rerun launch when &gt; 0.</param>
/// <param name="VacantForDrawLaunchCount">Places available for Rerun launch on an empty grid.</param>
public sealed record StageDrawBracketLaunchFactsDto(
    int EntryPlaceCount,
    int OccupiedByEntryCount,
    int ReservedByStructuralFeedCount,
    int FeedResolutionInvalidCount,
    int BlockingPlaceCount,
    int VacantForDrawLaunchCount);
