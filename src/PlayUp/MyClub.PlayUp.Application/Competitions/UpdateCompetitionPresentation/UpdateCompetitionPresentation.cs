// -----------------------------------------------------------------------
// <copyright file="UpdateCompetitionPresentation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: update competition short name and logo.
/// </summary>
public static class UpdateCompetitionPresentation
{
    /// <summary>
    /// Updates presentation; null clears a field.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="shortName">Short name, or null to clear.</param>
    /// <param name="logoPath">Logo path or absolute URI string, or null to clear.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, string? shortName, string? logoPath, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.UpdatePresentation(ShortName.Create(shortName), LogoUri.Create(logoPath), clock);
    }
}
