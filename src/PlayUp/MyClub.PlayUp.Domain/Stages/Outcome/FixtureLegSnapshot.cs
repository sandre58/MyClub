// -----------------------------------------------------------------------
// <copyright file="FixtureLegSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Immutable data for one confrontation leg (no sports validation in the constructor).
/// </summary>
public sealed record FixtureLegSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureLegSnapshot"/> class.
    /// </summary>
    public FixtureLegSnapshot(
        int legIndex,
        MatchId matchId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        MatchStatus status,
        Score? score,
        bool extraTimePlayed,
        PenaltyShootoutScore? penaltyShootoutScore = null)
    {
        LegIndex = legIndex;
        MatchId = matchId;
        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
        Status = status;
        Score = score;
        ExtraTimePlayed = extraTimePlayed;
        PenaltyShootoutScore = penaltyShootoutScore;
    }

    /// <summary>Gets the 1-based leg index.</summary>
    public int LegIndex { get; }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the home entry of this match.</summary>
    public EntryId HomeEntryId { get; }

    /// <summary>Gets the away entry of this match.</summary>
    public EntryId AwayEntryId { get; }

    /// <summary>Gets the match status.</summary>
    public MatchStatus Status { get; }

    /// <summary>Gets the play score when available.</summary>
    public Score? Score { get; }

    /// <summary>Gets a value indicating whether extra time was played on this match.</summary>
    public bool ExtraTimePlayed { get; }

    /// <summary>Gets the shootout score when taken.</summary>
    public PenaltyShootoutScore? PenaltyShootoutScore { get; }
}
