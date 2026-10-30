// -----------------------------------------------------------------------
// <copyright file="QualificationResolutionApplyWitness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Detects whether a qualification Apply has already written provenance for a source stage.
/// </summary>
internal static class QualificationResolutionApplyWitness
{
    /// <summary>
    /// Returns true when a destination occupant stores qualification provenance sourced from <paramref name="source"/>.
    /// </summary>
    internal static bool Exists(Stage source, IReadOnlyList<Stage>? competitionStages)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (competitionStages is null)
        {
            return false;
        }

        var prefix = $"Q|{source.Id.Value:N}|";
        foreach (var stage in competitionStages)
        {
            if (stage.Slots.Any(slot => IsQualificationFromSource(slot.ResolutionProvenance, prefix)))
            {
                return true;
            }

            if (stage.CompositionEntries.Any(entry => IsQualificationFromSource(entry.ResolutionProvenance, prefix)))
            {
                return true;
            }

            if (stage.GroupResolutionProvenances.Any(row =>
                    row.ProvenanceKind == ResolutionProvenanceKind.Qualification
                    && row.SourcePathId.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsQualificationFromSource(ResolutionProvenance? provenance, string prefix) =>
        provenance is { Kind: ResolutionProvenanceKind.Qualification } candidate
        && candidate.SourcePathId.StartsWith(prefix, StringComparison.Ordinal);
}
