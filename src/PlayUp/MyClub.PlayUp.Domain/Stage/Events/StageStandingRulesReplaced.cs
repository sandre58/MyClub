// -----------------------------------------------------------------------
// <copyright file="StageStandingRulesReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when standing rules of a stage are replaced (allowed after Start).
/// </summary>
public sealed record StageStandingRulesReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageStandingRulesReplaced"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageStandingRulesReplaced(StageId stageId, IClock clock)
        : base(clock) => StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
