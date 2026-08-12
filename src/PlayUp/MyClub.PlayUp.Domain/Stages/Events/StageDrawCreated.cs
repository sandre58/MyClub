// -----------------------------------------------------------------------
// <copyright file="StageDrawCreated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a draw is created on a stage.
/// </summary>
public sealed record StageDrawCreated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageDrawCreated"/> class.
    /// </summary>
    public StageDrawCreated(StageId stageId, DrawId drawId, DrawResolutionKind kind, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        DrawId = drawId;
        Kind = kind;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the draw identity.
    /// </summary>
    public DrawId DrawId { get; }

    /// <summary>
    /// Gets the draw resolution kind.
    /// </summary>
    public DrawResolutionKind Kind { get; }
}
