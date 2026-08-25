// -----------------------------------------------------------------------
// <copyright file="ScoreGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Deterministic football score generator.
/// </summary>
public static class ScoreGenerator
{
    /// <summary>
    /// Generates a home/away goal pair (typical amateur range).
    /// </summary>
    /// <param name="entropy">Entropy source.</param>
    /// <returns>Home and away goals.</returns>
    public static (int Home, int Away) Create(DeterministicEntropy entropy)
    {
        ArgumentNullException.ThrowIfNull(entropy);
        return (
            entropy.NextInclusive(0, 4),
            entropy.NextInclusive(0, 4));
    }
}
