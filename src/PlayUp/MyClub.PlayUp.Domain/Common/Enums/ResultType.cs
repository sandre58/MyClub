// -----------------------------------------------------------------------
// <copyright file="ResultType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// How a match result was obtained.
/// </summary>
public enum ResultType
{
    /// <summary>
    /// Match was played normally.
    /// </summary>
    Played = 0,

    /// <summary>
    /// One team declared a forfeit.
    /// </summary>
    Forfeit = 1,

    /// <summary>
    /// Administrative victory without playing the match.
    /// </summary>
    WalkOver = 2,

    /// <summary>
    /// Result decided by the organizer.
    /// </summary>
    Administrative = 3
}
