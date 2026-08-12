// -----------------------------------------------------------------------
// <copyright file="StandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Standings;

/// <summary>
/// One calculated standing line (position is derived, not a source of truth).
/// </summary>
public sealed record StandingRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandingRow"/> class.
    /// </summary>
    public StandingRow(
        EntryId entryId,
        int position,
        int played,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst,
        int points)
    {
        if (position < 1)
        {
            throw new DomainException(
                "Standing position must be at least 1.",
                StandingErrorCodes.ParticipantsInvalid);
        }

        EntryId = entryId;
        Position = position;
        Played = played;
        Wins = wins;
        Draws = draws;
        Losses = losses;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        Points = points;
    }

    /// <summary>
    /// Gets the entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the derived ranking position (1-based).
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets matches played.
    /// </summary>
    public int Played { get; }

    /// <summary>
    /// Gets wins.
    /// </summary>
    public int Wins { get; }

    /// <summary>
    /// Gets draws.
    /// </summary>
    public int Draws { get; }

    /// <summary>
    /// Gets losses.
    /// </summary>
    public int Losses { get; }

    /// <summary>
    /// Gets goals scored.
    /// </summary>
    public int GoalsFor { get; }

    /// <summary>
    /// Gets goals conceded.
    /// </summary>
    public int GoalsAgainst { get; }

    /// <summary>
    /// Gets goal difference (for − against).
    /// </summary>
    public int GoalDifference => GoalsFor - GoalsAgainst;

    /// <summary>
    /// Gets points.
    /// </summary>
    public int Points { get; }
}
