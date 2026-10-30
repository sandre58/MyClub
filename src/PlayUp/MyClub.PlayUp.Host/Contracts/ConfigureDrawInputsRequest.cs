// -----------------------------------------------------------------------
// <copyright file="ConfigureDrawInputsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for configuring default Draw inputs.
/// </summary>
/// <param name="Intent">
/// Default | Rerun | Partial (case-insensitive). Rerun ignores occupancy-derived Fixed*.
/// Partial draws unplaced teams into free slots and leaves occupants out of the pool.
/// Omitted or null → Default (Fixed* from occupancy).
/// </param>
public sealed record ConfigureDrawInputsRequest(string? Intent = null);
