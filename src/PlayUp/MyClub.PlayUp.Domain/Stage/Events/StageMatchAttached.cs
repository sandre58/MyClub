// -----------------------------------------------------------------------
// <copyright file="StageMatchAttached.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a match identity is attached to a fixture.
/// </summary>
public sealed record StageMatchAttached : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageMatchAttached"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageMatchAttached(StageId stageId, FixtureId fixtureId, MatchId matchId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        FixtureId = fixtureId;
        MatchId = matchId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the fixture identity.
    /// </summary>
    public FixtureId FixtureId { get; }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }
}
