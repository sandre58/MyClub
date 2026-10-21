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
/// Default | Rerun (case-insensitive). Rerun ignores occupancy-derived Fixed*.
/// Omitted or null → Default (Encoding F).
/// </param>
public sealed record ConfigureDrawInputsRequest(string? Intent = null);
