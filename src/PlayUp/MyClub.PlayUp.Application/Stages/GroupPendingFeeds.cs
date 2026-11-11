// -----------------------------------------------------------------------
// <copyright file="GroupPendingFeeds.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Unresolved qualification and progression feeds that still reserve a group place.
/// </summary>
/// <remarks>
/// A path is resolved when the target group still contains a member whose provenance
/// source path id is that path. The count is never negative.
/// </remarks>
internal static class GroupPendingFeeds
{
    internal static int CountUnresolved(
        Stage target,
        IReadOnlyList<Stage> competitionStages,
        GroupId groupId)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);
        var count = 0;
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                foreach (var path in qualification.Paths)
                {
                    if (TargetsGroup(path.Destination.StageId, path.Destination.TargetsGroup, path.Destination.GroupId, target.Id, groupId)
                        && !IsQualificationResolved(target, source.Id, path))
                    {
                        count++;
                    }
                }
            }

            if (source.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            foreach (var path in progression.Paths)
            {
                if (TargetsGroup(path.Destination.StageId, path.Destination.TargetsGroup, path.Destination.GroupId, target.Id, groupId)
                    && !IsProgressionResolved(target, source.Id, path))
                {
                    count++;
                }
            }
        }

        return count;
    }

    internal static bool IsQualificationResolved(Stage target, StageId sourceStageId, QualificationPath path)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(path);
        if (!path.Destination.TargetsGroup || path.Destination.GroupId is not { } groupId)
        {
            return false;
        }

        var key = ResolutionPathIdentifier.ForQualification(sourceStageId, path);
        return HasResolvedMember(target, groupId, ResolutionProvenanceKind.Qualification, key);
    }

    internal static bool IsProgressionResolved(Stage target, StageId sourceStageId, ProgressionPath path)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(path);
        if (!path.Destination.TargetsGroup || path.Destination.GroupId is not { } groupId)
        {
            return false;
        }

        var key = ResolutionPathIdentifier.ForProgression(sourceStageId, path);
        return HasResolvedMember(target, groupId, ResolutionProvenanceKind.Progression, key);
    }

    private static bool TargetsGroup(
        StageId destinationStageId,
        bool targetsGroup,
        GroupId? destinationGroupId,
        StageId targetStageId,
        GroupId groupId) =>
        targetsGroup
        && destinationStageId.Equals(targetStageId)
        && destinationGroupId is { } destination
        && destination.Equals(groupId);

    private static bool HasResolvedMember(
        Stage target,
        GroupId groupId,
        ResolutionProvenanceKind kind,
        string pathKey)
    {
        var group = target.FindGroup(groupId);
        if (group is null)
        {
            return false;
        }

        return target.GroupResolutionProvenances.Any(row =>
            row.GroupId.Equals(groupId)
            && row.ProvenanceKind == kind
            && string.Equals(row.SourcePathId, pathKey, StringComparison.Ordinal)
            && group.EntryIds.Contains(row.EntryId));
    }
}
