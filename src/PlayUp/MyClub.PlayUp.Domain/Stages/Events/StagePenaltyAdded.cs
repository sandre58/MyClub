// -----------------------------------------------------------------------
// <copyright file="StagePenaltyAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a standing penalty is added to a stage.
/// </summary>
public sealed record StagePenaltyAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StagePenaltyAdded"/> class.
    /// </summary>
    public StagePenaltyAdded(
        StageId stageId,
        PenaltyId penaltyId,
        EntryId entryId,
        int pointsDeducted,
        string? reason,
        IClock clock)
        : base(clock)
    {
        StageId = stageId;
        PenaltyId = penaltyId;
        EntryId = entryId;
        PointsDeducted = pointsDeducted;
        Reason = reason;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the penalty identity.
    /// </summary>
    public PenaltyId PenaltyId { get; }

    /// <summary>
    /// Gets the targeted entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the points deducted.
    /// </summary>
    public int PointsDeducted { get; }

    /// <summary>
    /// Gets the optional reason.
    /// </summary>
    public string? Reason { get; }
}
