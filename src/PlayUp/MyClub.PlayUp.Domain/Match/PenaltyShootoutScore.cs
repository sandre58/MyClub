// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutScore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match;

/// <summary>
/// Penalty shootout score (home and away converted kicks). Distinct from <see cref="Score"/> (play goals)
/// and from Standing Penalty entities.
/// </summary>
public readonly record struct PenaltyShootoutScore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PenaltyShootoutScore"/> struct.
    /// </summary>
    /// <param name="homeGoals">Converted kicks by the home entry (≥ 0).</param>
    /// <param name="awayGoals">Converted kicks by the away entry (≥ 0).</param>
    public PenaltyShootoutScore(int homeGoals, int awayGoals)
    {
        if (homeGoals < 0 || awayGoals < 0)
        {
            throw new DomainException(
                "Penalty shootout goals cannot be negative.",
                MatchErrorCodes.InvalidResult);
        }

        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    /// <summary>
    /// Gets the home converted kicks.
    /// </summary>
    public int HomeGoals { get; }

    /// <summary>
    /// Gets the away converted kicks.
    /// </summary>
    public int AwayGoals { get; }

    /// <inheritdoc />
    public override string ToString() => $"{HomeGoals}-{AwayGoals}";
}
