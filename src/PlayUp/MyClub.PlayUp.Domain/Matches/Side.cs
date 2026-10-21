// -----------------------------------------------------------------------
// <copyright file="Side.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Match side for a declared participation (home or away).
/// </summary>
public enum Side
{
    /// <summary>
    /// Home side of the match.
    /// </summary>
    Home = 0,

    /// <summary>
    /// Away side of the match.
    /// </summary>
    Away = 1
}
