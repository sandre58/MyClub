// -----------------------------------------------------------------------
// <copyright file="Score.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match;

/// <summary>
/// Match score (home and away goals).
/// </summary>
public readonly record struct Score
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Score"/> struct.
    /// </summary>
    /// <param name="homeGoals">Goals scored by the home entry (≥ 0).</param>
    /// <param name="awayGoals">Goals scored by the away entry (≥ 0).</param>
    public Score(int homeGoals, int awayGoals)
    {
        if (homeGoals < 0 || awayGoals < 0)
        {
            throw new DomainException(
                "Score goals cannot be negative.",
                MatchErrorCodes.InvalidResult);
        }

        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    /// <summary>
    /// Gets the home goals.
    /// </summary>
    public int HomeGoals { get; }

    /// <summary>
    /// Gets the away goals.
    /// </summary>
    public int AwayGoals { get; }

    /// <inheritdoc />
    public override string ToString() => $"{HomeGoals}-{AwayGoals}";
}
