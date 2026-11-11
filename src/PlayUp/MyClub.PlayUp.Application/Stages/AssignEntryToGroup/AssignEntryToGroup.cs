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
    /// <paramref name="competitionStages"/> supplies the qualification and progression paths that reserve group places.
    /// When omitted, only <paramref name="stage"/> is considered.
    /// </summary>
    public static void Execute(
        Stage stage,
        Competition competition,
        GroupId groupId,
        EntryId entryId,
        IReadOnlyList<Match>? stageMatches = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        _ = competition.GetEntry(entryId);
        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);
        var group = stage.FindGroup(groupId);
        if (group is not null)
        {
            var refusal = ManualGroupSeatRules.EvaluateAssign(
                stage,
                competitionStages ?? [stage],
                group,
                entryId,
                snapshots);
            if (refusal is not null)
            {
                ManualGroupSeatRules.Throw(refusal);
            }
        }

        stage.AssignManualEntryToGroup(groupId, entryId, snapshots);
    }
}
