// -----------------------------------------------------------------------
// <copyright file="StructureCompositionEligibility.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>Active vs ineligible members of a stage composition roster.</summary>
internal static class StructureCompositionEligibility
{
    internal static (int ActiveCount, int IneligibleCount) CountRoster(
        Competition competition,
        IReadOnlyList<CompositionEntry> compositionEntries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(compositionEntries);

        var entriesById = competition.Entries.ToDictionary(entry => entry.Id);
        var ineligible = 0;
        foreach (var compositionEntry in compositionEntries)
        {
            if (!entriesById.TryGetValue(compositionEntry.EntryId, out var entry)
                || entry.Status != EntryStatus.Active)
            {
                ineligible++;
            }
        }

        var active = compositionEntries.Count - ineligible;
        return (active, ineligible);
    }
}
