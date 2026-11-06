// -----------------------------------------------------------------------
// <copyright file="AssignEntryToGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: assign a competition entry to a group (manual placement).
/// </summary>
public static class AssignEntryToGroup
{
    /// <summary>
    /// Places <paramref name="entryId"/> in <paramref name="groupId"/> when allowed by population and draw seals.
    /// </summary>
    public static void Execute(
        Stage stage,
        Competition competition,
        GroupId groupId,
        EntryId entryId,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Group assignment");

        _ = competition.GetEntry(entryId);

        if (stage.CompositionEntries.All(row => !row.EntryId.Equals(entryId)))
        {
            throw new ApplicationFailureException(
                $"Entry '{entryId}' is not in the stage population.",
                ApplicationErrorCodes.GroupAssignmentEntryNotInComposition);
        }

        if (stage.GroupResolutionProvenances.Any(row =>
                row.EntryId.Equals(entryId)
                && row.ProvenanceKind is ResolutionProvenanceKind.Qualification
                    or ResolutionProvenanceKind.Progression))
        {
            throw new ApplicationFailureException(
                "This group placement was produced by qualification or progression and cannot be edited manually.",
                ApplicationErrorCodes.GroupManualEditResolutionLocked);
        }

        StructureAppliedDrawGroupPlacementGate.EnsureAssignAllowed(stage, groupId, entryId);

        stage.AssignEntryToGroup(
            groupId,
            entryId,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
    }
}
