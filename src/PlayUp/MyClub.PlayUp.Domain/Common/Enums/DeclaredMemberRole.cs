// -----------------------------------------------------------------------
// <copyright file="DeclaredMemberRole.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Nature of a declared member within a competition entry (not a career profession).
/// </summary>
public enum DeclaredMemberRole
{
    /// <summary>
    /// Declared as a player for this participation.
    /// </summary>
    Player = 0,

    /// <summary>
    /// Declared as team staff for this participation (not a match official).
    /// </summary>
    Staff = 1
}
