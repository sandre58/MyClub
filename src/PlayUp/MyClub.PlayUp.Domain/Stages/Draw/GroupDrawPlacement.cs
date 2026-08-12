// -----------------------------------------------------------------------
// <copyright file="GroupDrawPlacement.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Entry → Group placement used as a fixed Draw input and/or Group resolution result.
/// Applied via Stage.AssignEntryToGroup by Application — not via Slot.
/// </summary>
public sealed record GroupDrawPlacement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDrawPlacement"/> class.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="groupId">Destination group identity.</param>
    public GroupDrawPlacement(EntryId entryId, GroupId groupId)
    {
        EntryId = entryId;
        GroupId = groupId;
    }

    /// <summary>
    /// Gets the entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the destination group identity.
    /// </summary>
    public GroupId GroupId { get; }
}
