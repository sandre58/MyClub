// -----------------------------------------------------------------------
// <copyright file="AssignEntryToGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

        _ = competition.GetEntry(entryId);
        StructureAppliedDrawGroupPlacementGate.EnsureAssignAllowed(stage, groupId, entryId);
        stage.AssignManualEntryToGroup(
            groupId,
            entryId,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
    }
}
