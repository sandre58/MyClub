// -----------------------------------------------------------------------
// <copyright file="ClearEntryFromGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: remove a competition entry from a group (manual clear).
/// </summary>
public static class ClearEntryFromGroup
{
    /// <summary>
    /// Clears <paramref name="entryId"/> from <paramref name="groupId"/> when allowed.
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
        StructureAppliedDrawGroupPlacementGate.EnsureClearAllowed(stage, groupId, entryId);

        stage.RemoveEntryFromGroup(
            groupId,
            entryId,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
    }
}
