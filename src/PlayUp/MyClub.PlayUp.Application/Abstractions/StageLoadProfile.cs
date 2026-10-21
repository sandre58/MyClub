// -----------------------------------------------------------------------
// <copyright file="StageLoadProfile.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Controls how much of a Stage aggregate graph is loaded for a read query.
/// </summary>
public enum StageLoadProfile
{
    /// <summary>Root properties only (Id, Name, Status, …).</summary>
    Summary = 0,

    /// <summary>Structure + calendar placement (rounds, matchdays, fixtures, match placements).</summary>
    Structure = 1,

    /// <summary>Full aggregate graph (commands and competition-scoped reads).</summary>
    Full = 2
}
