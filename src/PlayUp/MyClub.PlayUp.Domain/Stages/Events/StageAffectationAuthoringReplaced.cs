// -----------------------------------------------------------------------
// <copyright file="StageAffectationAuthoringReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when the stage Affectation authoring set is replaced.
/// </summary>
public sealed record StageAffectationAuthoringReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageAffectationAuthoringReplaced"/> class.
    /// </summary>
    public StageAffectationAuthoringReplaced(StageId stageId, IClock clock)
        : base(clock) =>
        StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
