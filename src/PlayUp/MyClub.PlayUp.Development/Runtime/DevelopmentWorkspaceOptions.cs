// -----------------------------------------------------------------------
// <copyright file="DevelopmentWorkspaceOptions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Options for Development Workspace scenario execution (DevRunner / tests).
/// </summary>
public sealed class DevelopmentWorkspaceOptions
{
    /// <summary>Gets or sets the workspace seed used for determinism.</summary>
    public int Seed { get; set; } = 42;
}
