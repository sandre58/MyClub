// -----------------------------------------------------------------------
// <copyright file="StageMatchdayRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a matchday is removed from a stage.
/// </summary>
public sealed record StageMatchdayRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageMatchdayRemoved"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageMatchdayRemoved(StageId stageId, MatchdayId matchdayId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        MatchdayId = matchdayId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the matchday identity.
    /// </summary>
    public MatchdayId MatchdayId { get; }
}
