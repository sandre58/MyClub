// -----------------------------------------------------------------------
// <copyright file="MatchGenerationFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// How Fixtures/Matches are generated for Championship / Groups stages (not scheduling).
/// </summary>
/// <remarks>
/// Cup stages ignore this value. Knockout / Swiss modes are out of scope for V1.
/// </remarks>
public enum MatchGenerationFormat
{
    /// <summary>
    /// One directed pairing per unordered team pair (single round-robin).
    /// </summary>
    SingleRoundRobin = 0,

    /// <summary>
    /// Two directed pairings per unordered team pair with Home/Away mirrored (double round-robin).
    /// </summary>
    DoubleRoundRobin = 1
}
