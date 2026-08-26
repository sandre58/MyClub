// -----------------------------------------------------------------------
// <copyright file="UpdateEntryPresentation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: update entry presentation metadata.
/// </summary>
public static class UpdateEntryPresentation
{
    /// <summary>
    /// Updates entry presentation; null fields clear.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="shortName">Short name, or null to clear.</param>
    /// <param name="logoMediaId">Logo Media Guid, or null to clear.</param>
    /// <param name="primaryColor">Primary color, or null to clear.</param>
    /// <param name="secondaryColor">Secondary color, or null to clear.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        string? shortName,
        Guid? logoMediaId,
        string? primaryColor,
        string? secondaryColor,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.UpdateEntryPresentation(
            entryId,
            new EntryPresentation(
                ShortName.Create(shortName),
                LogoMediaId.Create(logoMediaId),
                TeamColor.Create(primaryColor),
                TeamColor.Create(secondaryColor)),
            clock);
    }
}
