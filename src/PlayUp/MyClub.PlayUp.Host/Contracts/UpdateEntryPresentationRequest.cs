// -----------------------------------------------------------------------
// <copyright file="UpdateEntryPresentationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for updating entry presentation (null clears a field).
/// </summary>
/// <param name="ShortName">Short name, or null to clear.</param>
/// <param name="LogoMediaId">Logo Media Guid, or null to clear.</param>
/// <param name="PrimaryColor">Primary color, or null to clear.</param>
/// <param name="SecondaryColor">Secondary color, or null to clear.</param>
public sealed record UpdateEntryPresentationRequest(
    string? ShortName,
    Guid? LogoMediaId,
    string? PrimaryColor,
    string? SecondaryColor);
