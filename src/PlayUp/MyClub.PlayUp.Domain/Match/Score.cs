// -----------------------------------------------------------------------
// <copyright file="Score.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match;

/// <summary>
/// Match play score (home and away goals at the end of play time).
/// </summary>
/// <remarks>
/// Represents goals scored during play at the end of the encounter: regulation time plus extra time when played.
/// Penalty shootout kicks are <strong>not</strong> included — see <see cref="PenaltyShootoutScore"/>.
/// </remarks>
public readonly record struct Score
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Score"/> struct.
    /// </summary>
    /// <param name="homeGoals">Play goals by the home entry (≥ 0; extra time included when played).</param>
    /// <param name="awayGoals">Play goals by the away entry (≥ 0; extra time included when played).</param>
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
