// -----------------------------------------------------------------------
// <copyright file="DrawConstraintType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Kind of draw constraint (closed set).
/// </summary>
public enum DrawConstraintType
{
    /// <summary>
    /// Avoid pairing the same team against itself (defensive / identity guard).
    /// </summary>
    SameTeamAvoidance = 0,

    /// <summary>
    /// Avoid pairing teams from the same group.
    /// </summary>
    SameGroupAvoidance = 1,

    /// <summary>
    /// Avoid pairing teams from the same association.
    /// </summary>
    SameAssociationAvoidance = 2,

    /// <summary>
    /// Limit how many entries sharing an association may sit in the same group (Group kind).
    /// Requires <see cref="DrawConstraint.MaxPerGroup"/>.
    /// </summary>
    MaxSameAssociationPerGroup = 3
}
