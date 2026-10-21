// -----------------------------------------------------------------------
// <copyright file="TeamNameSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Recipes;

/// <summary>
/// Source of team display names for a recipe.
/// </summary>
public enum TeamNameSource
{
    /// <summary>Generated credible fictional names.</summary>
    Generated = 0,

    /// <summary>Ordered local dataset list.</summary>
    Dataset = 1
}
