// -----------------------------------------------------------------------
// <copyright file="StageGroupAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage.Events;

/// <summary>
/// Raised when a group is added to a stage.
/// </summary>
public sealed record StageGroupAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageGroupAdded"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="groupId">The group identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageGroupAdded(StageId stageId, GroupId groupId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        GroupId = groupId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the group identity.
    /// </summary>
    public GroupId GroupId { get; }
}
