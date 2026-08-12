// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches.Events;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Aggregate root for a sporting encounter between two competition entries.
/// </summary>
[DebuggerDisplay("{HomeEntryId} vs {AwayEntryId} ({Status})")]
public sealed class Match : AggregateRoot<MatchId>
{
    private Match(
        MatchId id,
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId)
        : base(id)
    {
        CompetitionId = competitionId;
        StageId = stageId;
        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
        Status = MatchStatus.Scheduled;
    }

    /// <summary>
    /// Gets the owning competition identity (immutable).
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage identity (immutable).
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the home entry identity (immutable).
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry identity (immutable).
    /// </summary>
    public EntryId AwayEntryId { get; }

    /// <summary>
    /// Gets the match lifecycle status.
    /// </summary>
    public MatchStatus Status { get; private set; }

    /// <summary>
    /// Gets the match result when finished; otherwise <see langword="null"/>.
    /// </summary>
    public MatchResult? Result { get; private set; }

    /// <summary>
    /// Creates a new match in Scheduled status.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="homeEntryId">The home entry identity.</param>
    /// <param name="awayEntryId">The away entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created match.</returns>
    public static Match Create(
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (homeEntryId.Equals(awayEntryId))
        {
            throw new DomainException(
                "Home and away entries must be different.",
                MatchErrorCodes.SameParticipant);
        }

        var match = new Match(MatchId.New(), competitionId, stageId, homeEntryId, awayEntryId);
        match.Raise(new MatchCreated(
            match.Id,
            competitionId,
            stageId,
            homeEntryId,
            awayEntryId,
            clock));
        return match;
    }

    /// <summary>
    /// Starts the match (Scheduled to Live).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Start(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Scheduled, "Match can only be started from Scheduled.");
        Status = MatchStatus.Live;
        Raise(new MatchStarted(Id, clock));
    }

    /// <summary>
    /// Finishes the match with a result (Live to Finished).
    /// </summary>
    /// <param name="result">The match result.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void Finish(MatchResult result, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == MatchStatus.Finished || Result is not null)
        {
            throw new DomainException(
                "Match result is already recorded.",
                MatchErrorCodes.ResultAlreadyRecorded);
        }

        EnsureStatus(MatchStatus.Live, "Match can only be finished from Live.");

        Status = MatchStatus.Finished;
        Result = result;
        Raise(new MatchFinished(Id, result.Type, result.Score, clock));
    }

    /// <summary>
    /// Postpones the match (Scheduled to Postponed).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Postpone(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Scheduled, "Match can only be postponed from Scheduled.");
        Status = MatchStatus.Postponed;
        Raise(new MatchPostponed(Id, clock));
    }

    /// <summary>
    /// Returns a postponed match to Scheduled.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void ResumeSchedule(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Postponed, "Match can only resume schedule from Postponed.");
        Status = MatchStatus.Scheduled;
        Raise(new MatchResumed(Id, clock));
    }

    /// <summary>
    /// Cancels the match.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Cancel(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status is not (MatchStatus.Scheduled or MatchStatus.Live or MatchStatus.Postponed))
        {
            throw new DomainException(
                $"Match cannot be cancelled from '{Status}'.",
                MatchErrorCodes.InvalidTransition);
        }

        Status = MatchStatus.Cancelled;
        Raise(new MatchCancelled(Id, clock));
    }

    private void EnsureStatus(MatchStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message, MatchErrorCodes.InvalidTransition);
        }
    }
}
