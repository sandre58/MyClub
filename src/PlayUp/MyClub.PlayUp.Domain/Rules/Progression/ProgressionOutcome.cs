// -----------------------------------------------------------------------
// <copyright file="ProgressionOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Outcome of a Fixture/Tie used by progression and placement-award paths (never a single Match).
/// </summary>
/// <remarks>
/// Shared selector for <see cref="ProgressionPath"/> (slot routing) and
/// <see cref="PlacementAwardPath"/> (final rank). The two rule families remain distinct.
/// </remarks>
public enum ProgressionOutcome
{
    /// <summary>
    /// The winner of the confrontation.
    /// </summary>
    Winner = 0,

    /// <summary>
    /// The loser of the confrontation.
    /// </summary>
    Loser = 1
}
