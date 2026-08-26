// -----------------------------------------------------------------------
// <copyright file="SeedProgress.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// How far a structured competition seed should advance.
/// </summary>
public enum SeedProgress
{
    /// <summary>Structure ready; matches scheduled; no results.</summary>
    Prepared = 0,

    /// <summary>Approximately half of matches finished.</summary>
    Running = 1,

    /// <summary>All matches finished; competition completed.</summary>
    Finished = 2
}
