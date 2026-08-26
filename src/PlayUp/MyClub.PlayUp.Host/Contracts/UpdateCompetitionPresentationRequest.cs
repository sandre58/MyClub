// -----------------------------------------------------------------------
// <copyright file="UpdateCompetitionPresentationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for updating competition presentation.
/// </summary>
/// <param name="ShortName">Short name, or null to clear.</param>
/// <param name="LogoMediaId">Logo Media Guid, or null to clear.</param>
public sealed record UpdateCompetitionPresentationRequest(string? ShortName, Guid? LogoMediaId);
