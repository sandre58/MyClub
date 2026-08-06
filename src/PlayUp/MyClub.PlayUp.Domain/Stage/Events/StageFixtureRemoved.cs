// -----------------------------------------------------------------------
// <copyright file="StageFixtureRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a fixture is removed from a stage.
/// </summary>
public sealed record StageFixtureRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageFixtureRemoved"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageFixtureRemoved(StageId stageId, FixtureId fixtureId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        FixtureId = fixtureId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the fixture identity.
    /// </summary>
    public FixtureId FixtureId { get; }
}
