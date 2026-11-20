// -----------------------------------------------------------------------
// <copyright file="SetGroupEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: atomically set the whole group membership.
/// </summary>
public static class SetGroupEntries
{
    /// <summary>
    /// Maximum number of entries accepted in one request list.
    /// </summary>
    public const int MaxBatchSize = 64;

    /// <summary>
    /// Evaluates or applies the membership list. Returns <see langword="false"/> on a set noop (no persistence).
    /// </summary>
    public static bool Execute(
        Stage stage,
        Competition competition,
        GroupId groupId,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<EntryId>? expectedEntryIds,
        IReadOnlyList<Match>? stageMatches = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(entryIds);

        if (stage.FindManualPlacementLifecycleRefusal() is { } lifecycle)
        {
            var probe = entryIds.Count > 0 ? entryIds[0] : EntryId.New();
            throw new DomainException(
                stage.DescribeManualAssignRefusal(lifecycle, groupId, probe),
                lifecycle);
        }

        if (stage.FindGroup(groupId) is not { } group)
        {
            throw new DomainException(
                $"Group '{groupId}' was not found.",
                StageErrorCodes.GroupNotFound);
        }

        if (SetsEqual(group.EntryIds, entryIds))
        {
            return false;
        }

        if (expectedEntryIds is null)
        {
            throw new ApplicationFailureException(
                "A group membership mutation requires expectedEntryIds.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        if (!SetsEqual(group.EntryIds, expectedEntryIds))
        {
            throw new ApplicationFailureException(
                "The expected group membership is no longer current.",
                ApplicationErrorCodes.GroupMembershipStale);
        }

        if (stage.PlacesPerGroup is not { } capacity || capacity < 1)
        {
            throw new ApplicationFailureException(
                "Setting group membership requires PlacesPerGroup of at least 1.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []);
        var stages = competitionStages ?? [stage];
        var current = group.EntryIds.ToHashSet();
        var target = entryIds.ToHashSet();

        foreach (var entryId in group.EntryIds)
        {
            if (target.Contains(entryId))
            {
                continue;
            }

            var refusal = ManualGroupSeatRules.EvaluateClear(stage, groupId, entryId);
            if (refusal is not null)
            {
                ManualGroupSeatRules.ThrowClear(stage, groupId, entryId, refusal);
            }
        }

        var projected = group.EntryIds.Count;
        foreach (var entryId in entryIds)
        {
            if (current.Contains(entryId))
            {
                continue;
            }

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
                projectedMemberCount: projected,
                deferCapacityChecks: true);
            if (refusal is not null)
            {
                ManualGroupSeatRules.ThrowBatch(stage, groupId, entryId, refusal);
            }

            projected++;
        }

        EvaluateNetCapacity(stage, stages, group, entryIds, out var ceilingCode, out var ceilingEntryId);
        if (ceilingCode is not null && ceilingEntryId is { } refused)
        {
            ManualGroupSeatRules.ThrowBatch(stage, groupId, refused, ceilingCode);
        }

        stage.ApplyManualGroupMembership(groupId, BuildFinalOrder(group.EntryIds, entryIds));
        return true;
    }

    internal static bool SetsEqual(IReadOnlyList<EntryId> left, IReadOnlyList<EntryId> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var set = left.ToHashSet();
        return set.Count == left.Count && set.SetEquals(right);
    }

    private static List<EntryId> BuildFinalOrder(
        IReadOnlyList<EntryId> current,
        IReadOnlyList<EntryId> requested)
    {
        var requestedSet = requested.ToHashSet();
        var final = current.Where(requestedSet.Contains).ToList();

        var present = final.ToHashSet();
        final.AddRange(requested.Where(present.Add));

        return final;
    }

    private static void EvaluateNetCapacity(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        Group group,
        IReadOnlyList<EntryId> requested,
        out string? code,
        out EntryId? entryId)
    {
        code = null;
        entryId = null;
        if (stage.PlacesPerGroup is not { } capacity || capacity < 1)
        {
            return;
        }

        var currentCount = group.EntryIds.Count;
        var finalCount = requested.Count;
        var pending = GroupPendingFeeds.CountUnresolved(stage, competitionStages, group.Id);
        var fullCap = Math.Max(currentCount, capacity);
        if (finalCount > fullCap && pending == 0)
        {
            code = StageErrorCodes.GroupFull;
            entryId = FirstAddition(group.EntryIds, requested);
            return;
        }

        var reservedCap = Math.Max(currentCount, Math.Max(0, capacity - pending));
        if (finalCount <= reservedCap) return;
        code = pending > 0
            ? ApplicationErrorCodes.GroupSeatReservedByInboundFeed
            : StageErrorCodes.GroupFull;
        entryId = FirstAddition(group.EntryIds, requested);
    }

    private static EntryId FirstAddition(IReadOnlyList<EntryId> current, IReadOnlyList<EntryId> requested)
    {
        var members = current.ToHashSet();
        foreach (var entryId in requested)
        {
            if (!members.Contains(entryId))
            {
                return entryId;
            }
        }

        return requested[0];
    }
}
