// -----------------------------------------------------------------------
// <copyright file="FeedKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Configuration mechanism that feeds a slot (never derived from <see cref="Slot.EntryId"/>).
/// </summary>
public enum FeedKind
{
    /// <summary>
    /// Ranking-based qualification path.
    /// </summary>
    Qualification = 0,

    /// <summary>
    /// Fixture/Tie outcome progression path.
    /// </summary>
    Progression = 1,

    /// <summary>
    /// Draw target (Entity Draw future; snapshot placeholder in V1).
    /// </summary>
    Draw = 2,

    /// <summary>
    /// Explicit direct assignment configuration.
    /// </summary>
    Direct = 3
}
