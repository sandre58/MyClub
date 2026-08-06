// -----------------------------------------------------------------------
// <copyright file="CompetitionStageAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competition.Events;

/// <summary>
/// Raised when a stage is added to a competition.
/// </summary>
public sealed record CompetitionStageAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionStageAdded"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionStageAdded(CompetitionId competitionId, StageId stageId, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        StageId = stageId;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
