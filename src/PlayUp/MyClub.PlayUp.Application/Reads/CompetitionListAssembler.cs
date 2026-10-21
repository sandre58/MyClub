// -----------------------------------------------------------------------
// <copyright file="CompetitionListAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles Competition List rows from Domain aggregates (no persisted read model).
/// </summary>
public static class CompetitionListAssembler
{
    /// <summary>
    /// Maps competitions to list items (stable name then id order expected from caller).
    /// </summary>
    /// <param name="competitions">Competitions to project.</param>
    /// <returns>List item DTOs.</returns>
    public static IReadOnlyList<CompetitionListItemDto> Assemble(IReadOnlyList<Competition> competitions)
    {
        ArgumentNullException.ThrowIfNull(competitions);

        return [.. competitions.Select(competition =>
            new CompetitionListItemDto(
                competition.Id.Value,
                competition.Name.Value,
                competition.Status,
                competition.ShortName?.Value,
                competition.LogoMediaId?.Value,
                competition.ScheduledStart,
                competition.ScheduledEnd))];
    }
}
