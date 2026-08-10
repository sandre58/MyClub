// -----------------------------------------------------------------------
// <copyright file="StandingMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Standing;

/// <summary>
/// Data-only finished match snapshot for standing calculation (no aggregate references).
/// </summary>
public sealed record StandingMatch
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandingMatch"/> class.
    /// </summary>
    /// <param name="homeEntryId">Home entry.</param>
    /// <param name="awayEntryId">Away entry.</param>
    /// <param name="homeGoals">Home goals (≥ 0).</param>
    /// <param name="awayGoals">Away goals (≥ 0).</param>
    public StandingMatch(EntryId homeEntryId, EntryId awayEntryId, int homeGoals, int awayGoals)
    {
        if (homeEntryId.Equals(awayEntryId))
        {
            throw new DomainException(
                "Standing match participants must be different.",
                StandingErrorCodes.ParticipantsInvalid);
        }

        if (homeGoals < 0 || awayGoals < 0)
        {
            throw new DomainException(
                "Standing match goals cannot be negative.",
                StandingErrorCodes.ParticipantsInvalid);
        }

        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    /// <summary>
    /// Gets the home entry identity.
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry identity.
    /// </summary>
    public EntryId AwayEntryId { get; }

    /// <summary>
    /// Gets the home goals.
    /// </summary>
    public int HomeGoals { get; }

    /// <summary>
    /// Gets the away goals.
    /// </summary>
    public int AwayGoals { get; }
}
