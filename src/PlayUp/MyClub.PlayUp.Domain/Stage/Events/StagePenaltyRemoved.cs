// -----------------------------------------------------------------------
// <copyright file="StagePenaltyRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a standing penalty is removed from a stage (delete revocation).
/// </summary>
public sealed record StagePenaltyRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StagePenaltyRemoved"/> class.
    /// </summary>
    public StagePenaltyRemoved(
        StageId stageId,
        PenaltyId penaltyId,
        EntryId entryId,
        int pointsDeducted,
        IClock clock)
        : base(clock)
    {
        StageId = stageId;
        PenaltyId = penaltyId;
        EntryId = entryId;
        PointsDeducted = pointsDeducted;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the removed penalty identity.
    /// </summary>
    public PenaltyId PenaltyId { get; }

    /// <summary>
    /// Gets the targeted entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the points that were deducted.
    /// </summary>
    public int PointsDeducted { get; }
}
