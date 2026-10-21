// -----------------------------------------------------------------------
// <copyright file="ProgressionPathSourceKey.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stable source identity for a progression path (P-4 diff).
/// </summary>
public readonly record struct ProgressionPathSourceKey(string SourcePairKey, ProgressionOutcome Outcome)
{
    /// <summary>
    /// Creates a key from a path.
    /// </summary>
    public static ProgressionPathSourceKey FromPath(ProgressionPath path) =>
        new(path.SourcePairKey, path.Outcome);
}
