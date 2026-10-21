// -----------------------------------------------------------------------
// <copyright file="StageDrawPublished.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a draw is published (immutable resolution snapshot).
/// Publish ≠ Apply: materialization is <c>ApplyDraw</c> (Application) and does not change draw status.
/// </summary>
public sealed record StageDrawPublished : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageDrawPublished"/> class.
    /// </summary>
    public StageDrawPublished(StageId stageId, DrawId drawId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        DrawId = drawId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the draw identity.
    /// </summary>
    public DrawId DrawId { get; }
}
