// -----------------------------------------------------------------------
// <copyright file="StagePrepared.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a stage is prepared (Draft to Ready).
/// </summary>
public sealed record StagePrepared : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StagePrepared"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StagePrepared(StageId stageId, IClock clock)
        : base(clock) => StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
