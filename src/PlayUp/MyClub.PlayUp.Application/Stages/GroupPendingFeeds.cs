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
/// One enumeration serves the reserved-place guard and the schematic.
/// A path is resolved when the target group still contains a member whose provenance
/// source path id is that path. The count is never negative.
/// </remarks>
internal static class GroupPendingFeeds
{
    /// <summary>One unresolved ForGroup path into <see cref="GroupId"/>.</summary>
    /// <param name="GroupId">The destination group.</param>
    /// <param name="Source">The stage that owns the path.</param>
    /// <param name="Qualification">The qualification path when this feed is a qualification.</param>
    /// <param name="Progression">The progression path when this feed is a progression.</param>
    internal readonly record struct UnresolvedFeed(
        GroupId GroupId,
        Stage Source,
        QualificationPath? Qualification,
        ProgressionPath? Progression);

    internal static IReadOnlyList<UnresolvedFeed> List(
        Stage target,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);
        var feeds = new List<UnresolvedFeed>();
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                foreach (var path in qualification.Paths)
                {
                    if (!TargetsThisStage(path.Destination.StageId, path.Destination.TargetsGroup, target.Id)
                        || IsQualificationResolved(target, source.Id, path)
                        || path.Destination.GroupId is not { } groupId)
                    {
                        continue;
                    }

                    feeds.Add(new UnresolvedFeed(groupId, source, path, Progression: null));
                }
            }

            if (source.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            foreach (var path in progression.Paths)
            {
                if (!TargetsThisStage(path.Destination.StageId, path.Destination.TargetsGroup, target.Id)
                    || IsProgressionResolved(target, source.Id, path)
                    || path.Destination.GroupId is not { } groupId)
                {
                    continue;
                }

                feeds.Add(new UnresolvedFeed(groupId, source, Qualification: null, path));
            }
        }

        return feeds;
    }

    internal static int CountUnresolved(
        Stage target,
        IReadOnlyList<Stage> competitionStages,
        GroupId groupId) =>
        List(target, competitionStages).Count(feed => feed.GroupId.Equals(groupId));

    private static bool IsQualificationResolved(Stage target, StageId sourceStageId, QualificationPath path)
    {
        if (!path.Destination.TargetsGroup || path.Destination.GroupId is not { } groupId)
        {
            return false;
        }

        var key = ResolutionPathIdentifier.ForQualification(sourceStageId, path);
        return HasResolvedMember(target, groupId, ResolutionProvenanceKind.Qualification, key);
    }

    private static bool IsProgressionResolved(Stage target, StageId sourceStageId, ProgressionPath path)
    {
        if (!path.Destination.TargetsGroup || path.Destination.GroupId is not { } groupId)
        {
            return false;
        }

        var key = ResolutionPathIdentifier.ForProgression(sourceStageId, path);
        return HasResolvedMember(target, groupId, ResolutionProvenanceKind.Progression, key);
    }

    private static bool TargetsThisStage(StageId destinationStageId, bool targetsGroup, StageId targetStageId) =>
        targetsGroup && destinationStageId.Equals(targetStageId);

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
