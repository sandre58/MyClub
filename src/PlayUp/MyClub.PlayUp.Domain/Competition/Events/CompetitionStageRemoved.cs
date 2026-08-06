// -----------------------------------------------------------------------
// <copyright file="CompetitionStageRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competition.Events;

/// <summary>
/// Raised when a stage is removed from a competition.
/// </summary>
public sealed record CompetitionStageRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionStageRemoved"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionStageRemoved(CompetitionId competitionId, StageId stageId, IClock clock)
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
