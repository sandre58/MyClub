// -----------------------------------------------------------------------
// <copyright file="StageStarted.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a stage starts (Ready to Running).
/// </summary>
public sealed record StageStarted : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageStarted"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageStarted(StageId stageId, IClock clock)
        : base(clock) => StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
