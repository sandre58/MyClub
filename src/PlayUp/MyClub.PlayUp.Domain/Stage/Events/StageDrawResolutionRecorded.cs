// -----------------------------------------------------------------------
// <copyright file="StageDrawResolutionRecorded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a draw resolution is recorded or marked NoSolution.
/// </summary>
public sealed record StageDrawResolutionRecorded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageDrawResolutionRecorded"/> class.
    /// </summary>
    public StageDrawResolutionRecorded(
        StageId stageId,
        DrawId drawId,
        DrawResolutionState resolutionState,
        IClock clock)
        : base(clock)
    {
        StageId = stageId;
        DrawId = drawId;
        ResolutionState = resolutionState;
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
    /// Gets the recorded resolution state.
    /// </summary>
    public DrawResolutionState ResolutionState { get; }
}
