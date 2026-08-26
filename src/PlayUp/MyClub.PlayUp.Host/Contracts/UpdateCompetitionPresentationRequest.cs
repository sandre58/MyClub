// -----------------------------------------------------------------------
// <copyright file="UpdateCompetitionPresentationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for updating competition presentation (null clears a field).
/// </summary>
/// <param name="ShortName">Short name, or null to clear.</param>
/// <param name="LogoPath">Logo path or absolute URI string, or null to clear.</param>
public sealed record UpdateCompetitionPresentationRequest(string? ShortName, string? LogoPath);
