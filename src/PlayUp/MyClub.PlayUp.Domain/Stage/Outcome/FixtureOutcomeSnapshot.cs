// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Immutable data contract for single-leg fixture outcome resolution (no sports validation in the constructor).
/// </summary>
/// <remarks>
/// Application is responsible for Fixture↔Match coherence (MatchId in Fixture.MatchIds; V1 exactly one match).
/// Resolution rules live in <see cref="FixtureOutcomeResolver"/>.
/// Does not carry <c>ExtraTimePlayed</c> — outcome depends only on play <see cref="Score"/> and optional shootout.
/// </remarks>
public sealed record FixtureOutcomeSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureOutcomeSnapshot"/> class.
    /// </summary>
    /// <param name="fixtureId">Fixture identity.</param>
    /// <param name="matchId">Single-leg match identity.</param>
    /// <param name="homeEntryId">Home participant.</param>
    /// <param name="awayEntryId">Away participant.</param>
    /// <param name="status">Match status.</param>
    /// <param name="score">Final play score when finished; otherwise <see langword="null"/>.</param>
    /// <param name="penaltyShootoutScore">Shootout score when taken; otherwise <see langword="null"/>.</param>
    public FixtureOutcomeSnapshot(
        FixtureId fixtureId,
        MatchId matchId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        MatchStatus status,
        Score? score,
        PenaltyShootoutScore? penaltyShootoutScore = null)
    {
        FixtureId = fixtureId;
        MatchId = matchId;
        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
        Status = status;
        Score = score;
        PenaltyShootoutScore = penaltyShootoutScore;
    }

    /// <summary>
    /// Gets the fixture identity.
    /// </summary>
    public FixtureId FixtureId { get; }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the home entry identity.
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry identity.
    /// </summary>
    public EntryId AwayEntryId { get; }

    /// <summary>
    /// Gets the match status.
    /// </summary>
    public MatchStatus Status { get; }

    /// <summary>
    /// Gets the play score when available.
    /// </summary>
    public Score? Score { get; }

    /// <summary>
    /// Gets the penalty shootout score when taken; otherwise <see langword="null"/>.
    /// </summary>
    public PenaltyShootoutScore? PenaltyShootoutScore { get; }
}
