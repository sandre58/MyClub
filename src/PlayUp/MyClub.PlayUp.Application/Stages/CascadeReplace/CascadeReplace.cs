// -----------------------------------------------------------------------
// <copyright file="CascadeReplace.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;
using ApplicationFailureException = MyClub.PlayUp.Application.ApplicationFailureException;

namespace MyClub.PlayUp.Application.Stages.CascadeReplace;

/// <summary>
/// Replaces one stale resolution and the stale resolutions that depend on it, in topological order.
/// Direct assignments are never written. A completed phase blocks the command.
/// </summary>
public static class CascadeReplace
{
    /// <summary>
    /// Replays the confirmed occupant and the stale chain around it.
    /// Callers persist with a single save. This method does not.
    /// </summary>
    public static CascadeReplaceResult Execute(
        Competition competition,
        StageId originStageId,
        CascadeReplaceTarget target,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(clock);

        var token = CascadeReplaceVersion.Compute(stages, matches);
        var order = Plan(competition, originStageId, target, stages, matches);
        var played = new List<CascadePlayedMatch>();
        var replacements = new List<CascadeReplacement>();
        foreach (var node in order)
        {
            Apply(node, competition, stages, matches, clock, played, replacements);
        }

        return new CascadeReplaceResult(token, played, replacements);
    }

    private static List<ResolutionNode> Plan(
        Competition competition,
        StageId originStageId,
        CascadeReplaceTarget target,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches)
    {
        var assessments = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches));
        var selected = assessments.FirstOrDefault(assessment => IsTarget(assessment, originStageId, target));
        if (selected is null)
        {
            throw Fail("The resolution occupant was not found.", ApplicationErrorCodes.CascadeReplaceOccupantNotFound);
        }

        if (selected.Kind == ResolutionProvenanceKind.Directe)
        {
            throw Fail(
                "A direct assignment is never replaced.",
                ApplicationErrorCodes.CascadeReplaceDirectAssignment);
        }

        if (selected.Kind is not (ResolutionProvenanceKind.Qualification or ResolutionProvenanceKind.Progression))
        {
            throw Fail(
                "Only a qualification or progression occupant can be replaced.",
                ApplicationErrorCodes.CascadeReplaceNotReplayable);
        }

        if (selected.State != StructureResolutionFreshness.Perime)
        {
            throw Fail(
                "The occupant is not stale.",
                ApplicationErrorCodes.CascadeReplaceNotStale);
        }

        var nodes = assessments
            .Where(assessment => assessment.Kind is ResolutionProvenanceKind.Qualification
                or ResolutionProvenanceKind.Progression)
            .GroupBy(assessment => (assessment.StageId, assessment.SourcePathId))
            .Select(group => new ResolutionNode(group.Key.StageId, group.Key.SourcePathId, [.. group]))
            .ToList();
        var origin = nodes.Single(node =>
            node.StageId == selected.StageId && node.SourcePathId == selected.SourcePathId);
        origin.IsOrigin = true;

        var forward = Edges(nodes);
        var closure = Closure(origin, nodes, forward);
        if (HasCycle(closure, forward))
        {
            throw Fail("The resolution chain contains a cycle.", ApplicationErrorCodes.CascadeReplaceCycle);
        }

        foreach (var node in closure)
        {
            var stage = stages.First(candidate => candidate.Id.Value == node.StageId);
            if (stage.Status == StageStatus.Completed)
            {
                throw Fail(
                    $"Phase '{stage.Id}' is completed and blocks the cascade.",
                    ApplicationErrorCodes.CascadeReplaceCompleted);
            }

            if (node.Members.Any(member =>
                    member.Target == StructureResolutionOccupantTarget.Slot
                    && member.SlotKey is { } slotKey
                    && stage.DirectAssignments.Any(assignment =>
                        string.Equals(assignment.SlotKey, slotKey, StringComparison.Ordinal))))
            {
                throw Fail(
                    "A direct assignment is never replaced.",
                    ApplicationErrorCodes.CascadeReplaceDirectAssignment);
            }
        }

        return Topological(closure, forward);
    }

    private static void Apply(
        ResolutionNode node,
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock,
        List<CascadePlayedMatch> played,
        List<CascadeReplacement> replacements)
    {
        var assessments = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches));
        var members = assessments
            .Where(assessment =>
                assessment.StageId == node.StageId
                && assessment.SourcePathId == node.SourcePathId
                && assessment.Kind is ResolutionProvenanceKind.Qualification or ResolutionProvenanceKind.Progression)
            .ToList();
        if (members.Count == 0)
        {
            return;
        }

        var replay = members.FirstOrDefault(member => member.ReplayEntryId is not null && member.ReplayFingerprint is not null);
        if (replay?.ReplayEntryId is not { } replayId || replay.ReplayFingerprint is not { } fingerprint)
        {
            if (node.IsOrigin)
            {
                throw Fail(
                    "The selection is indeterminate and cannot be replaced.",
                    ApplicationErrorCodes.CascadeReplaceIndeterminate);
            }

            return;
        }

        var stage = stages.First(candidate => candidate.Id.Value == node.StageId);
        var next = new EntryId(replayId);
        var stageMatches = matches.Where(match => match.StageId.Equals(stage.Id)).ToArray();
        var provenance = new ResolutionProvenance(replay.Kind, replay.SourcePathId, fingerprint);
        var slot = members.FirstOrDefault(member =>
            member.Target == StructureResolutionOccupantTarget.Slot && member.SlotKey is not null);
        var group = members.FirstOrDefault(member =>
            member.Target == StructureResolutionOccupantTarget.Group && member.GroupId is not null);

        if (slot?.SlotKey is { } slotKey)
        {
            CascadeMatchAlignment.AlignSlot(stage, slotKey, next, stageMatches, clock, played);
        }

        if (group is not null)
        {
            CascadeMatchAlignment.AlignEntry(stage, new EntryId(group.EntryId), next, stageMatches, clock, played);
        }

        var snapshots = MaterializedParticipantSnapshots.FromMatches(stageMatches);
        stage.AddResolvedPopulationEntry(next, clock, provenance);

        if (slot?.SlotKey is { } key)
        {
            var before = stage.FindSlot(key)?.EntryId;
            stage.ApplyResolvedEntry(key, next, clock, provenance, snapshots);
            if (before is { } previous && !previous.Equals(next))
            {
                replacements.Add(new CascadeReplacement(
                    stage.Id.Value,
                    StructureResolutionOccupantTarget.Slot,
                    key,
                    null,
                    previous.Value,
                    next.Value));
            }
        }

        if (group?.GroupId is { } groupValue)
        {
            var groupId = new GroupId(groupValue);
            var previous = new EntryId(group.EntryId);
            if (!previous.Equals(next))
            {
                stage.RemoveResolvedGroupEntry(groupId, previous, snapshots);
            }

            stage.ApplyResolvedGroupEntry(groupId, next, provenance);
            if (!previous.Equals(next))
            {
                replacements.Add(new CascadeReplacement(
                    stage.Id.Value,
                    StructureResolutionOccupantTarget.Group,
                    null,
                    groupValue,
                    previous.Value,
                    next.Value));
            }
        }

        foreach (var previousId in members.Select(member => member.EntryId).Distinct())
        {
            if (previousId == next.Value)
            {
                continue;
            }

            var previous = new EntryId(previousId);
            foreach (var form in stage.FormPathResolutions.Where(row => row.EntryId.Equals(previous)).ToArray())
            {
                stage.RecordFormPathResolution(form.PathFingerprint, next);
            }

            if (Referenced(stage, previous))
            {
                continue;
            }

            stage.RemoveResolvedPopulationEntry(previous, clock);
            replacements.Add(new CascadeReplacement(
                stage.Id.Value,
                StructureResolutionOccupantTarget.Population,
                null,
                null,
                previous.Value,
                next.Value));
        }
    }

    private static bool Referenced(Stage stage, EntryId entryId) =>
        stage.Slots.Any(slot => slot.EntryId is { } occupant && occupant.Equals(entryId))
        || stage.Groups.Any(group => group.EntryIds.Contains(entryId))
        || stage.DirectAssignments.Any(assignment => assignment.EntryId.Equals(entryId));

    private static bool IsTarget(
        ResolutionOccupantAssessment assessment,
        StageId originStageId,
        CascadeReplaceTarget target) =>
        assessment.StageId == originStageId.Value
        && assessment.Target == target.Target
        && assessment.EntryId == target.EntryId
        && (target.Target != StructureResolutionOccupantTarget.Slot
            || string.Equals(assessment.SlotKey, target.SlotKey, StringComparison.Ordinal))
        && (target.Target != StructureResolutionOccupantTarget.Group
            || assessment.GroupId == target.GroupId);

    private static Dictionary<ResolutionNode, List<ResolutionNode>> Edges(IReadOnlyList<ResolutionNode> nodes)
    {
        var edges = nodes.ToDictionary(node => node, _ => new List<ResolutionNode>());
        foreach (var upstream in nodes)
        {
            foreach (var downstream in nodes)
            {
                if (ReferenceEquals(upstream, downstream))
                {
                    continue;
                }

                if (downstream.DependsOnStageIds.Contains(upstream.StageId)
                    && downstream.DependsOnEntryIds.Contains(upstream.EntryId))
                {
                    edges[upstream].Add(downstream);
                }
            }
        }

        return edges;
    }

    private static List<ResolutionNode> Closure(
        ResolutionNode origin,
        IReadOnlyList<ResolutionNode> nodes,
        Dictionary<ResolutionNode, List<ResolutionNode>> forward)
    {
        var stale = nodes.Where(node => node.Stale).ToHashSet();
        var reverse = nodes.ToDictionary(node => node, _ => new List<ResolutionNode>());
        foreach (var (upstream, destinations) in forward)
        {
            foreach (var downstream in destinations)
            {
                reverse[downstream].Add(upstream);
            }
        }

        var set = new HashSet<ResolutionNode> { origin };
        var queue = new Queue<ResolutionNode>();
        queue.Enqueue(origin);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in forward[current].Concat(reverse[current]))
            {
                if (!stale.Contains(next) || !set.Add(next))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        return [.. set];
    }

    private static bool HasCycle(
        IReadOnlyList<ResolutionNode> closure,
        Dictionary<ResolutionNode, List<ResolutionNode>> forward)
    {
        var inSet = closure.ToHashSet();
        var color = closure.ToDictionary(node => node, _ => 0);
        foreach (var node in closure)
        {
            if (color[node] == 0 && Visit(node))
            {
                return true;
            }
        }

        return false;

        bool Visit(ResolutionNode node)
        {
            color[node] = 1;
            foreach (var next in forward[node])
            {
                if (!inSet.Contains(next))
                {
                    continue;
                }

                if (color[next] == 1 || (color[next] == 0 && Visit(next)))
                {
                    return true;
                }
            }

            color[node] = 2;
            return false;
        }
    }

    private static List<ResolutionNode> Topological(
        IReadOnlyList<ResolutionNode> closure,
        Dictionary<ResolutionNode, List<ResolutionNode>> forward)
    {
        var inSet = closure.ToHashSet();
        var incoming = closure.ToDictionary(node => node, _ => 0);
        foreach (var node in closure)
        {
            foreach (var next in forward[node])
            {
                if (inSet.Contains(next))
                {
                    incoming[next]++;
                }
            }
        }

        var queue = new Queue<ResolutionNode>(incoming.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var ordered = new List<ResolutionNode>(closure.Count);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            ordered.Add(node);
            foreach (var next in forward[node])
            {
                if (!inSet.Contains(next))
                {
                    continue;
                }

                incoming[next]--;
                if (incoming[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        return ordered;
    }

    private static Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries(IReadOnlyList<Match> matches) =>
        matches
            .GroupBy(match => match.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MatchSummaryRow>)group.Select(match => new MatchSummaryRow(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive)).ToArray());

    private static ApplicationFailureException Fail(string message, string code) =>
        new(message, code);

    private sealed class ResolutionNode(
        Guid stageId,
        string sourcePathId,
        IReadOnlyList<ResolutionOccupantAssessment> members)
    {
        public Guid StageId { get; } = stageId;

        public string SourcePathId { get; } = sourcePathId;

        public IReadOnlyList<ResolutionOccupantAssessment> Members { get; } = members;

        public bool IsOrigin { get; set; }

        public bool Stale => Members.Any(member => member.State == StructureResolutionFreshness.Perime);

        public Guid EntryId => Members[0].EntryId;

        public HashSet<Guid> DependsOnStageIds { get; } =
            [.. members.Where(member => member.DependsOnStageId is not null).Select(member => member.DependsOnStageId!.Value)];

        public HashSet<Guid> DependsOnEntryIds { get; } =
            [.. members.SelectMany(member => member.DependsOnEntryIds)];
    }
}
