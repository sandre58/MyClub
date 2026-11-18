// -----------------------------------------------------------------------
// <copyright file="AssignEntriesToGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: place several entries in one group, all or nothing.
/// </summary>
public static class AssignEntriesToGroup
{
    /// <summary>
    /// Maximum number of entries accepted in one lot.
    /// </summary>
    public const int MaxBatchSize = 64;

    /// <summary>
    /// Places <paramref name="entryIds"/> after the current members, in list order.
    /// An entry already in the group is skipped and does not consume a place.
    /// The lot is refused, with no membership change, when <c>PlacesPerGroup</c> is absent or below 1,
    /// or when any new entry would pass the free places left after unresolved inbound feeds.
    /// </summary>
    /// <param name="stage">Phase that owns the group.</param>
    /// <param name="competition">Competition that owns the entries.</param>
    /// <param name="groupId">Target group.</param>
    /// <param name="entryIds">Entries in the order they should be appended.</param>
    /// <param name="stageMatches">Matches attached on the phase. Empty when none.</param>
    /// <param name="competitionStages">
    /// Phases whose qualification and progression paths reserve places. When omitted, only
    /// <paramref name="stage"/> is considered.
    /// </param>
    public static void Execute(
        Stage stage,
        Competition competition,
        GroupId groupId,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<Match>? stageMatches = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);
        if (entryIds.Count == 0 || entryIds.Count > MaxBatchSize || HasDuplicate(entryIds))
        {
            throw new ApplicationFailureException(
                "A group lot requires a non-empty list of distinct entries, at most 64.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        if (stage.PlacesPerGroup is not int capacity || capacity < 1)
        {
            throw new ApplicationFailureException(
                "A group lot requires PlacesPerGroup of at least 1.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);
        var stages = competitionStages ?? [stage];
        if (stage.FindGroup(groupId) is not { } group)
        {
            ManualGroupSeatRules.ThrowBatch(stage, groupId, entryIds[0], StageErrorCodes.GroupNotFound);
            return;
        }

        var projected = group.EntryIds.Count;
        foreach (var entryId in entryIds)
        {
            try
            {
                _ = competition.GetEntry(entryId);
            }
            catch (DomainException exception) when (exception.Code == CompetitionErrorCodes.EntryNotFound)
            {
                throw new GroupPlacementRejectedException(exception.Message, exception.Code, entryId);
            }

            var refusal = ManualGroupSeatRules.EvaluateAssign(
                stage,
                stages,
                group,
                entryId,
                snapshots,
                projectedMemberCount: projected);
            if (refusal is not null)
            {
                ManualGroupSeatRules.ThrowBatch(stage, groupId, entryId, refusal);
            }

            if (!group.EntryIds.Contains(entryId))
            {
                projected++;
            }
        }

        stage.AssignManualEntriesToGroup(groupId, entryIds, snapshots);
    }

    private static bool HasDuplicate(IReadOnlyList<EntryId> entryIds)
    {
        var seen = new HashSet<EntryId>();
        foreach (var entryId in entryIds)
        {
            if (!seen.Add(entryId))
            {
                return true;
            }
        }

        return false;
    }
}
