// -----------------------------------------------------------------------
// <copyright file="ReplaceEntryInGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace the team on a group seat without moving the seat.
/// </summary>
public static class ReplaceEntryInGroup
{
    /// <summary>
    /// Replaces <paramref name="currentEntryId"/> with <paramref name="replacementEntryId"/> on
    /// <paramref name="groupId"/> when the seat can be cleared. A full group and a reserved feed
    /// do not refuse the replacement. Replacing a team with itself does nothing.
    /// When the occupant is already gone and the replacement is already in the group, the call
    /// does nothing: the wanted state is already there.
    /// </summary>
    /// <param name="stage">Phase that owns the group.</param>
    /// <param name="competition">Competition that owns the replacement.</param>
    /// <param name="groupId">Target group.</param>
    /// <param name="currentEntryId">Occupant named by the route.</param>
    /// <param name="replacementEntryId">Entry that takes the seat.</param>
    /// <param name="stageMatches">Matches attached on the phase. Empty when none.</param>
    public static void Execute(
        Stage stage,
        Competition competition,
        GroupId groupId,
        EntryId currentEntryId,
        EntryId replacementEntryId,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        _ = competition.GetEntry(replacementEntryId);
        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);
        if (ManualGroupSeatRules.EvaluateReplace(
                stage,
                groupId,
                currentEntryId,
                replacementEntryId,
                snapshots) is { } refusal)
        {
            ManualGroupSeatRules.ThrowReplace(stage, groupId, currentEntryId, replacementEntryId, refusal);
        }

        stage.ReplaceManualEntryInGroup(groupId, currentEntryId, replacementEntryId, snapshots);
    }
}
