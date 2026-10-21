// -----------------------------------------------------------------------
// <copyright file="MatchLoadProfile.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Controls how much match data is loaded for a competition-scoped read bundle.
/// </summary>
public enum MatchLoadProfile
{
    /// <summary>No match data.</summary>
    None = 0,

    /// <summary>Status-only rows for completion diagnostics.</summary>
    StatusOnly = 1,

    /// <summary>Minimal slice for Needs Attention (status, sides, result when finished).</summary>
    AttentionSlice = 2,

    /// <summary>List projection without sheet owned collections.</summary>
    SummaryRow = 3,

    /// <summary>Full match aggregates (sheet collections hydrated).</summary>
    Full = 4
}
