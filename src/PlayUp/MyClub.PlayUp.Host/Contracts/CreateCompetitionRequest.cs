// -----------------------------------------------------------------------
// <copyright file="CreateCompetitionRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for creating a Competition (bootstrap).
/// </summary>
/// <param name="Name">Display name (Domain <c>CompetitionName</c> validates).</param>
public sealed record CreateCompetitionRequest(string Name);
