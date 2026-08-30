// -----------------------------------------------------------------------
// <copyright file="RunningScore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Current play score of a match that has been started (not the official finished result).
/// </summary>
/// <remarks>
/// Distinct from <see cref="Score"/>, which is the official play score inside <see cref="MatchResult"/>.
/// No shootout, extra-time period semantics, or nominative goal attribution.
/// </remarks>
public readonly record struct RunningScore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunningScore"/> struct.
    /// </summary>
    /// <param name="homeGoals">Current home goals (≥ 0).</param>
    /// <param name="awayGoals">Current away goals (≥ 0).</param>
    public RunningScore(int homeGoals, int awayGoals)
    {
        if (homeGoals < 0 || awayGoals < 0)
        {
            throw new DomainException(
                "Running score goals cannot be negative.",
                MatchErrorCodes.InvalidRunningScore);
        }

        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    /// <summary>
    /// Gets the current home goals.
    /// </summary>
    public int HomeGoals { get; }

    /// <summary>
    /// Gets the current away goals.
    /// </summary>
    public int AwayGoals { get; }

    /// <inheritdoc />
    public override string ToString() => $"{HomeGoals}-{AwayGoals}";
}
