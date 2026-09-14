// -----------------------------------------------------------------------
// <copyright file="StageCompositionEntriesReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when the root composition entry set of a stage is replaced.
/// </summary>
public sealed record StageCompositionEntriesReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageCompositionEntriesReplaced"/> class.
    /// </summary>
    public StageCompositionEntriesReplaced(StageId stageId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
