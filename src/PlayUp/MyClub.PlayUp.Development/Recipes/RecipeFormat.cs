// -----------------------------------------------------------------------
// <copyright file="RecipeFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Recipes;

/// <summary>
/// V1 single-stage structure formats (aligned with Application <c>StructureFormatKind</c>).
/// </summary>
public enum RecipeFormat
{
    /// <summary>Championship matchdays.</summary>
    Championship = 0,

    /// <summary>Group stage.</summary>
    Groups = 1,

    /// <summary>Cup / knockout bracket.</summary>
    Cup = 2,

    /// <summary>Swiss classique — progressive Matchdays via GenerateNextRound.</summary>
    Swiss = 3
}
