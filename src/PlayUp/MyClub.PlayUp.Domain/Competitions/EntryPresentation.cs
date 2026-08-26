// -----------------------------------------------------------------------
// <copyright file="EntryPresentation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Optional presentation metadata for a competition entry.
/// </summary>
/// <param name="ShortName">Abbreviated name.</param>
/// <param name="LogoUri">Logo path or URI.</param>
/// <param name="PrimaryColor">Primary kit color.</param>
/// <param name="SecondaryColor">Secondary kit color.</param>
public sealed record EntryPresentation(
    ShortName? ShortName = null,
    LogoUri? LogoUri = null,
    TeamColor? PrimaryColor = null,
    TeamColor? SecondaryColor = null)
{
    /// <summary>
    /// Gets an empty presentation (all fields null).
    /// </summary>
    public static EntryPresentation Empty { get; } = new();
}
