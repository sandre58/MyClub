// -----------------------------------------------------------------------
// <copyright file="StageMatchdayAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a matchday is added to a stage.
/// </summary>
public sealed record StageMatchdayAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageMatchdayAdded"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageMatchdayAdded(StageId stageId, MatchdayId matchdayId, IClock clock)
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
