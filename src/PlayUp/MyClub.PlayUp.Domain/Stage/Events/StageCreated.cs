// -----------------------------------------------------------------------
// <copyright file="StageCreated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a stage is created.
/// </summary>
public sealed record StageCreated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageCreated"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="competitionId">The owning competition identity.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageCreated(StageId stageId, CompetitionId competitionId, string name, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        CompetitionId = competitionId;
        Name = name;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage name.
    /// </summary>
    public string Name { get; }
}
