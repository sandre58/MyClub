// -----------------------------------------------------------------------
// <copyright file="StageDrawCancelled.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a draw is cancelled (rerun requires a new Draw).
/// </summary>
public sealed record StageDrawCancelled : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageDrawCancelled"/> class.
    /// </summary>
    public StageDrawCancelled(StageId stageId, DrawId drawId, IClock clock)
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
