// -----------------------------------------------------------------------
// <copyright file="EntryStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Participation status of a CompetitionEntry.
/// </summary>
public enum EntryStatus
{
    /// <summary>
    /// Entry is actively competing.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Entry has qualified to a further stage.
    /// </summary>
    Qualified = 1,

    /// <summary>
    /// Entry has been eliminated.
    /// </summary>
    Eliminated = 2,

    /// <summary>
    /// Entry has withdrawn from the competition.
    /// </summary>
    Withdrawn = 3,

    /// <summary>
    /// Entry has been excluded by the organizer.
    /// </summary>
    Excluded = 4
}
