// -----------------------------------------------------------------------
// <copyright file="EntrySideDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// One side of a match (entry identity + presentation).
/// </summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name when known.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
/// <param name="SecondaryColor">Optional secondary kit color (#RRGGBB).</param>
public sealed record EntrySideDto(
    Guid EntryId,
    string? DisplayName,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null);
