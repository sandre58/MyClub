// -----------------------------------------------------------------------
// <copyright file="CompetitionScheduleSet.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when declared competition schedule dates are set or cleared.
/// </summary>
public sealed record CompetitionScheduleSet : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionScheduleSet"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="scheduledStart">Declared start, if any.</param>
    /// <param name="scheduledEnd">Declared end, if any.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionScheduleSet(
        CompetitionId competitionId,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the declared start, if any.
    /// </summary>
    public DateTimeOffset? ScheduledStart { get; }

    /// <summary>
    /// Gets the declared end, if any.
    /// </summary>
    public DateTimeOffset? ScheduledEnd { get; }
}
