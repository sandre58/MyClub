// -----------------------------------------------------------------------
// <copyright file="ProgressionOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Outcome of a Fixture/Tie used by a progression path (never a single Match).
/// </summary>
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
