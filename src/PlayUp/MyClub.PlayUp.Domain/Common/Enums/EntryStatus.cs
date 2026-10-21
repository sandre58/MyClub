// -----------------------------------------------------------------------
// <copyright file="EntryStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Participation status of a <c>CompetitionEntry</c>.
/// Sporting progression (qualified / eliminated) lives on Qualification / Progression — not here.
/// </summary>
public enum EntryStatus
{
    /// <summary>
    /// Entry is actively competing.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Entry has declared a forfait (withdrawn) during the competition. Still occupies a place.
    /// </summary>
    Withdrawn = 3
}
