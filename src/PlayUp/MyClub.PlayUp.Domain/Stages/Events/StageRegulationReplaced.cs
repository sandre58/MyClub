// -----------------------------------------------------------------------
// <copyright file="StageRegulationReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a stage regulation is replaced as a whole.
/// </summary>
public sealed record StageRegulationReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRegulationReplaced"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageRegulationReplaced(StageId stageId, IClock clock)
        : base(clock) => StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
