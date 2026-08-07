// -----------------------------------------------------------------------
// <copyright file="RankingScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Ranking scope used as the origin of a qualification selection.
/// </summary>
public enum RankingScope
{
    /// <summary>
    /// Ranking within a specific group (<see cref="QualificationSource.GroupId"/> required).
    /// </summary>
    Group = 0,

    /// <summary>
    /// Overall ranking across the stage (no group id).
    /// </summary>
    Overall = 1
}
