// -----------------------------------------------------------------------
// <copyright file="MatchEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

/// <summary>
/// Abstract base class for events that occur during a football match.
/// Match events represent significant occurrences during the course of a match that need to be recorded,
/// such as goals, cards, substitutions, and other notable incidents.
/// </summary>
public abstract class MatchEvent : Entity<MatchEventId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected MatchEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchEvent"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the match event.</param>
    /// <param name="minute">The minute of the match when this event occurred. Can be null for events without specific timing.</param>
    protected MatchEvent(MatchEventId id, int? minute = null)
        : base(id) => Minute = minute;

    /// <summary>
    /// Gets or sets the minute of the match when this event occurred.
    /// This can be null for events that don't have a specific time reference or occur outside regular match time.
    /// </summary>
    public int? Minute { get; set; }

    /// <summary>
    /// Gets the identifier of the match to which this event belongs.
    /// This property is used internally by Entity Framework Core to maintain the relationship between events and matches.
    /// </summary>
    [UsedImplicitly(Reason = "Used by EF Core.")]
    internal MatchId MatchId { get; private set; } = null!;
}
