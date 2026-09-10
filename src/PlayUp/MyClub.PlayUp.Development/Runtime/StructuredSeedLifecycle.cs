// -----------------------------------------------------------------------
// <copyright file="StructuredSeedLifecycle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// How far a mono structured seed advances beyond structure + materialization.
/// </summary>
public enum StructuredSeedLifecycle
{
    /// <summary>Prepare + Start, then apply <see cref="SeedProgress"/> (default).</summary>
    Progressive = 0,

    /// <summary>Materialize (when applicable) then Prepare only — competition/stage Ready.</summary>
    Ready = 1,

    /// <summary>Progressive Running mid-state, then Suspend competition + stage.</summary>
    Suspended = 2,

    /// <summary>Progressive Finished, then Archive competition.</summary>
    Archived = 3
}
