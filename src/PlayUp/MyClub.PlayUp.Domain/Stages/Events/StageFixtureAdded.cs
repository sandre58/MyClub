// -----------------------------------------------------------------------
// <copyright file="StageFixtureAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a fixture is added to a stage.
/// </summary>
public sealed record StageFixtureAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageFixtureAdded"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageFixtureAdded(StageId stageId, FixtureId fixtureId, IClock clock)
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
