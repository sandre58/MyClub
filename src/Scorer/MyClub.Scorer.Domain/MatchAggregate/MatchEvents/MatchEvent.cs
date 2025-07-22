// -----------------------------------------------------------------------
// <copyright file="MatchEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public abstract class MatchEvent : Entity<MatchEventId>
{
    // <remarks>Used by EF Core</remarks>
    protected MatchEvent()
        : base() { }

    protected MatchEvent(MatchEventId id, int? minute = null)
        : base(id) => Minute = minute;

    public int? Minute { get; set; }
}
