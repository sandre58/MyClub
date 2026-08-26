// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for materializing Cup Fixtures/Matches from occupied bracket slots.
/// </summary>
/// <param name="Pairs">Explicit SlotA/SlotB confrontations (Home/Away convention).</param>
public sealed record MaterializeCupFromOccupiedSlotsRequest(
    IReadOnlyList<CupSlotPairRequest> Pairs);

/// <summary>
/// One bracket confrontation to materialize.
/// </summary>
/// <param name="SlotAKey">Home-side slot key.</param>
/// <param name="SlotBKey">Away-side slot key.</param>
public sealed record CupSlotPairRequest(string SlotAKey, string SlotBKey);
