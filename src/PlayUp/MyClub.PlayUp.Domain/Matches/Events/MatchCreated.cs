// -----------------------------------------------------------------------
// <copyright file="MatchCreated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a match is created (status Scheduled).
/// </summary>
public sealed record MatchCreated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchCreated"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="homeEntryId">The home entry identity.</param>
    /// <param name="awayEntryId">The away entry identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchCreated(
        MatchId matchId,
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        CompetitionId = competitionId;
        StageId = stageId;
        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
    }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the home entry identity.
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry identity.
    /// </summary>
    public EntryId AwayEntryId { get; }
}
