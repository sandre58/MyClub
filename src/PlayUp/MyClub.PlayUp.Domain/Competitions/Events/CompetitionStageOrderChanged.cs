// -----------------------------------------------------------------------
// <copyright file="CompetitionStageOrderChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when the ordered list of stage ids is changed.
/// </summary>
public sealed record CompetitionStageOrderChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionStageOrderChanged"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageIds">The new ordered stage identities.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionStageOrderChanged(CompetitionId competitionId, IReadOnlyList<StageId> stageIds, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        StageIds = stageIds;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the new ordered stage identities.
    /// </summary>
    public IReadOnlyList<StageId> StageIds { get; }
}
