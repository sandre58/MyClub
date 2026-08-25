// -----------------------------------------------------------------------
// <copyright file="ScenarioCategory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// High-level scenario family for catalog filtering.
/// </summary>
public enum ScenarioCategory
{
    /// <summary>Empty workspace / list.</summary>
    Workspace = 0,

    /// <summary>Construction / organisation.</summary>
    Construction = 1,

    /// <summary>Running competition.</summary>
    Operational = 2,

    /// <summary>Completed / archived.</summary>
    Terminal = 3,

    /// <summary>Seed with non-deterministic-looking but seed-driven variety.</summary>
    Random = 4,
}
