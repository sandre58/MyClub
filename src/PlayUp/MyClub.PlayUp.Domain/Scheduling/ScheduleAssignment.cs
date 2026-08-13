// -----------------------------------------------------------------------
// <copyright file="ScheduleAssignment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// One match placement: resource and start. End is derived from the match duration in context.
/// </summary>
public sealed record ScheduleAssignment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleAssignment"/> class.
    /// </summary>
    public ScheduleAssignment(MatchId matchId, ResourceId resourceId, DateTimeOffset start)
    {
        MatchId = matchId;
        ResourceId = resourceId;
        Start = start;
    }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the assigned resource identity.
    /// </summary>
    public ResourceId ResourceId { get; }

    /// <summary>
    /// Gets the occupation start instant.
    /// </summary>
    public DateTimeOffset Start { get; }
}
