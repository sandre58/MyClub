// -----------------------------------------------------------------------
// <copyright file="CupSlotPair.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Explicit bracket confrontation: two occupied stage slots become one Fixture.
/// </summary>
/// <param name="SlotAKey">Home-side bracket slot key.</param>
/// <param name="SlotBKey">Away-side bracket slot key.</param>
public sealed record CupSlotPair(string SlotAKey, string SlotBKey);
