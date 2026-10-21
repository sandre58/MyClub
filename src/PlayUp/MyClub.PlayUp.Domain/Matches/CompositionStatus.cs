// -----------------------------------------------------------------------
// <copyright file="CompositionStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Declared composition status of a participation on a match sheet (not live on-field presence).
/// </summary>
public enum CompositionStatus
{
    /// <summary>
    /// Declared as a starter for this match.
    /// </summary>
    Starter = 0,

    /// <summary>
    /// Declared as a bench player for this match.
    /// </summary>
    Bench = 1
}
