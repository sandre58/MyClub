// -----------------------------------------------------------------------
// <copyright file="GroupEntryRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Infrastructure row for ordered group entry identities (Domain Group.EntryIds).
/// </summary>
internal sealed class GroupEntryRef
{
    /// <summary>
    /// Gets or sets the owning group identity.
    /// </summary>
    public GroupId GroupId { get; set; }

    /// <summary>
    /// Gets or sets the competition entry identity.
    /// </summary>
    public EntryId EntryId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based sort order.
    /// </summary>
    public int SortOrder { get; set; }
}
