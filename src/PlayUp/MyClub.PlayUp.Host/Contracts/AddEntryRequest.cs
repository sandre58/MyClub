// -----------------------------------------------------------------------
// <copyright file="AddEntryRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a competition entry.
/// </summary>
/// <param name="DisplayName">Entry display name.</param>
/// <param name="TeamId">Optional team identity; generated when omitted.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoPath">Optional logo path or absolute URI string.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
/// <param name="SecondaryColor">Optional secondary kit color (#RRGGBB).</param>
public sealed record AddEntryRequest(
    string DisplayName,
    Guid? TeamId = null,
    string? ShortName = null,
    string? LogoPath = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null);
