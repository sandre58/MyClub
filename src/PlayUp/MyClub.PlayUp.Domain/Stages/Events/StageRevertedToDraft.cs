// -----------------------------------------------------------------------
// <copyright file="StageRevertedToDraft.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a stage reverts from Ready to Draft.
/// </summary>
public sealed record StageRevertedToDraft : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRevertedToDraft"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageRevertedToDraft(StageId stageId, IClock clock)
        : base(clock) => StageId = stageId;

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }
}
