// -----------------------------------------------------------------------
// <copyright file="EntryPresentation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Presentation metadata for a competition entry.
/// </summary>
/// <param name="ShortName">Required abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media reference for the entry logo.</param>
/// <param name="PrimaryColor">Primary kit color.</param>
/// <param name="SecondaryColor">Secondary kit color.</param>
public sealed record EntryPresentation(
    ShortName ShortName,
    LogoMediaId? LogoMediaId = null,
    TeamColor? PrimaryColor = null,
    TeamColor? SecondaryColor = null);
