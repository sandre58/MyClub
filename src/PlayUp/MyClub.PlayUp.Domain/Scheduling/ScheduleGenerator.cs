// -----------------------------------------------------------------------
// <copyright file="ScheduleGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Pure Domain service: exhaustive deterministic scheduling under Required constraints.
/// Does not mutate Existing; no IClock, RNG, Stage, Match aggregate, Draw, Qualification, Progression, or Standing.
/// </summary>
public static class ScheduleGenerator
{
    /// <summary>
    /// Generates a complete schedule for the targets, or NoSolution / InvalidRequest.
    /// </summary>
    public static SchedulingResult Generate(SchedulingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<SchedulingValidationError>();
        if (!TryValidateRequest(request, errors))
        {
            return SchedulingResult.InvalidRequest(errors);
        }

        var matchById = request.Matches.ToDictionary(m => m.MatchId);
        var resourceById = request.Resources.ToDictionary(r => r.ResourceId);
        var targets = request.TargetMatchIds.ToArray();
        var targetSet = targets.ToHashSet();

        var fixedAssignments = request.Existing.Assignments
            .Where(a => !targetSet.Contains(a.MatchId))
            .ToArray();

        if (!TryValidateFixed(request, fixedAssignments, matchById, resourceById, errors))
        {
            return SchedulingResult.InvalidRequest(errors);
        }

        var sameStartParent = BuildSameStartParents(request.SameStarts, matchById.Keys);
        var componentBound = new Dictionary<MatchId, DateTimeOffset?>();
        foreach (var root in matchById.Keys.Select(matchId => FindRoot(sameStartParent, matchId)))
        {
            componentBound.TryAdd(root, null);
        }

        foreach (var fixedAssignment in fixedAssignments)
        {
            var root = FindRoot(sameStartParent, fixedAssignment.MatchId);
            if (componentBound.TryGetValue(root, out var bound) && bound is not null && bound != fixedAssignment.Start)
            {
                return SchedulingResult.InvalidRequest(
                [
                    Err("Fixed SameStart starts conflict.", fixedAssignment.MatchId.ToString())
                ]);
            }

            componentBound[root] = fixedAssignment.Start;
        }

        var precedences = NormalizePrecedences(request.Precedences);
        var separations = NormalizeSeparations(request.MinimumSeparations);
        var gap = request.MinimumGapBetweenMatches;

        if (!TryValidateFixedConstraints(
                fixedAssignments,
                matchById,
                gap,
                precedences,
                separations,
                sameStartParent,
                errors))
        {
            return SchedulingResult.InvalidRequest(errors);
        }

        var domains = new Dictionary<MatchId, List<Candidate>>();
        foreach (var targetId in targets)
        {
            domains[targetId] = BuildDomain(request, matchById[targetId], resourceById);
        }

        var state = SearchState.Create(fixedAssignments, matchById, componentBound, sameStartParent);
        return Search(0, targets, domains, matchById, gap, precedences, separations, state)
            ? SchedulingResult.Success(state.ToSchedule())
            : SchedulingResult.NoSolution();
    }

    private static bool Search(
        int index,
        MatchId[] targets,
        Dictionary<MatchId, List<Candidate>> domains,
        Dictionary<MatchId, MatchSchedulingContext> matchById,
        MinimumGapBetweenMatches? gap,
        IReadOnlyList<Precedence> precedences,
        IReadOnlyList<MinimumSeparation> separations,
        SearchState state)
    {
        if (index >= targets.Length)
        {
            return true;
        }

        var matchId = targets[index];
        var context = matchById[matchId];
        var root = FindRoot(state.SameStartParent, matchId);
        var bound = state.ComponentBound[root];

        foreach (var candidate in domains[matchId].Where(candidate => (bound is null || candidate.Start == bound.Value) && IsFeasible(matchId, context, candidate, gap, precedences, separations, state)))
        {
            state.Apply(matchId, candidate, root, bound is null);
            if (Search(index + 1, targets, domains, matchById, gap, precedences, separations, state))
            {
                return true;
            }

            state.Undo();
        }

        return false;
    }

    private static bool IsFeasible(
        MatchId matchId,
        MatchSchedulingContext context,
        Candidate candidate,
        MinimumGapBetweenMatches? gap,
        IReadOnlyList<Precedence> precedences,
        IReadOnlyList<MinimumSeparation> separations,
        SearchState state)
    {
        var end = candidate.Start + context.Duration.AsTimeSpan();

        if (!RespectsResourceOccupancy(candidate.ResourceId, candidate.Start, end, gap, state))
        {
            return false;
        }

        foreach (var precedence in precedences)
        {
            if (precedence.PredecessorMatchId.Equals(matchId))
            {
                if (state.TryGetOccupation(precedence.SuccessorMatchId, out var succ)
                    && candidate.Start + context.Duration.AsTimeSpan() + precedence.MinimumDelay > succ.Start)
                {
                    return false;
                }
            }
            else if (precedence.SuccessorMatchId.Equals(matchId)
                     && state.TryGetOccupation(precedence.PredecessorMatchId, out var pred)
                     && candidate.Start < pred.End + precedence.MinimumDelay)
            {
                return false;
            }
        }

        foreach (var separation in separations)
        {
            MatchId otherId;
            if (separation.MatchA.Equals(matchId))
            {
                otherId = separation.MatchB;
            }
            else if (separation.MatchB.Equals(matchId))
            {
                otherId = separation.MatchA;
            }
            else
            {
                continue;
            }

            if (!state.TryGetOccupation(otherId, out var other))
            {
                continue;
            }

            if (!RespectsSeparation(candidate.Start, end, other.Start, other.End, separation.AsTimeSpan()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RespectsResourceOccupancy(
        ResourceId resourceId,
        DateTimeOffset start,
        DateTimeOffset end,
        MinimumGapBetweenMatches? gap,
        SearchState state)
    {
        var gapSpan = gap?.AsTimeSpan() ?? TimeSpan.Zero;
        var requireGap = gap is not null;

        foreach (var occupation in state.OccupationsOn(resourceId))
        {
            if (Overlaps(start, end, occupation.Start, occupation.End))
            {
                return false;
            }

            if (requireGap
                && !RespectsSeparation(start, end, occupation.Start, occupation.End, gapSpan))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RespectsSeparation(
        DateTimeOffset aStart,
        DateTimeOffset aEnd,
        DateTimeOffset bStart,
        DateTimeOffset bEnd,
        TimeSpan separation) =>
        aEnd + separation <= bStart || bEnd + separation <= aStart;

    private static bool Overlaps(
        DateTimeOffset aStart,
        DateTimeOffset aEnd,
        DateTimeOffset bStart,
        DateTimeOffset bEnd) =>
        aStart < bEnd && bStart < aEnd;

    private static List<Candidate> BuildDomain(
        SchedulingRequest request,
        MatchSchedulingContext match,
        Dictionary<ResourceId, ResourceSchedulingContext> resourceById)
    {
        var duration = match.Duration.AsTimeSpan();
        var rawStarts = BuildRawStarts(request, match, duration);
        var resources = ResolveResources(match, request.Resources);

        var result = new List<Candidate>();
        foreach (var resourceId in resources)
        {
            var availability = resourceById[resourceId].AvailabilityWindows;
            result.AddRange(from start in rawStarts let end = start + duration where FitsAvailability(availability, start, end) select new Candidate(resourceId, start));
        }

        return result;
    }

    private static List<DateTimeOffset> BuildRawStarts(
        SchedulingRequest request,
        MatchSchedulingContext match,
        TimeSpan duration)
    {
        var h0 = request.Horizon.Start;
        var h1 = request.Horizon.End;
        var starts = new List<DateTimeOffset>();

        if (match.ImposedStart is { } imposed)
        {
            if (imposed + duration <= h1 && imposed >= h0)
            {
                starts.Add(imposed);
            }
        }
        else
        {
            var step = request.TimeGranularity.AsTimeSpan();
            for (var start = h0; start + duration <= h1; start += step)
            {
                starts.Add(start);
            }
        }

        return match.AllowedStartWindows is null
            ? starts
            : [
            .. starts
                .Where(start => match.AllowedStartWindows.Any(w => w.ContainsOccupation(start, start + duration)))
        ];
    }

    private static IReadOnlyList<ResourceId> ResolveResources(
        MatchSchedulingContext match,
        IReadOnlyList<ResourceSchedulingContext> resources) =>
        match.AllowedResourceIds ?? [.. resources.Select(r => r.ResourceId)];

    private static bool FitsAvailability(
        IReadOnlyList<TimeWindow> windows,
        DateTimeOffset start,
        DateTimeOffset end) =>
        windows.Any(window => window.ContainsOccupation(start, end));

    private static bool TryValidateRequest(SchedulingRequest request, List<SchedulingValidationError> errors)
    {
        if (request.Horizon.Start > request.Horizon.End)
        {
            errors.Add(Err("Horizon start must not be greater than end."));
        }

        if (request.Matches.Select(m => m.MatchId).Distinct().Count() != request.Matches.Count)
        {
            errors.Add(Err("Match contexts must have unique match identities."));
        }

        if (request.Resources.Select(r => r.ResourceId).Distinct().Count() != request.Resources.Count)
        {
            errors.Add(Err("Resource contexts must have unique resource identities."));
        }

        if (request.TargetMatchIds.Distinct().Count() != request.TargetMatchIds.Count)
        {
            errors.Add(Err("Target match ids cannot contain duplicates."));
        }

        var matchIds = request.Matches.Select(m => m.MatchId).ToHashSet();
        var resourceIds = request.Resources.Select(r => r.ResourceId).ToHashSet();

        foreach (var match in request.Matches)
        {
            if (match.Home.Kind == MatchParticipantKind.MissingExpected
                || match.Away.Kind == MatchParticipantKind.MissingExpected)
            {
                errors.Add(Err("Participant is missing but expected.", match.MatchId.ToString()));
            }

            if (match.AllowedStartWindows is { Count: 0 })
            {
                errors.Add(Err("AllowedStartWindows cannot be an empty list.", match.MatchId.ToString()));
            }

            if (match.AllowedStartWindows is not null
                && !AreWindowsStrictlyDisjointNonAdjacent(match.AllowedStartWindows))
            {
                errors.Add(Err("AllowedStartWindows must be strictly disjoint and non-adjacent.", match.MatchId.ToString()));
            }

            if (match.AllowedResourceIds is not null)
            {
                errors.AddRange(from resourceId in match.AllowedResourceIds where !resourceIds.Contains(resourceId) select Err("Allowed resource is unknown.", resourceId.ToString()));
            }
        }

        errors.AddRange(from resource in request.Resources where !AreWindowsStrictlyDisjointNonAdjacent(resource.AvailabilityWindows) select Err("Availability windows must be strictly disjoint and non-adjacent.", resource.ResourceId.ToString()));

        errors.AddRange(from targetId in request.TargetMatchIds where !matchIds.Contains(targetId) select Err("Target match has no scheduling context.", targetId.ToString()));

        foreach (var assignment in request.Existing.Assignments)
        {
            if (!matchIds.Contains(assignment.MatchId))
            {
                errors.Add(Err("Existing assignment match has no scheduling context.", assignment.MatchId.ToString()));
            }

            if (!resourceIds.Contains(assignment.ResourceId))
            {
                errors.Add(Err("Existing assignment resource is unknown.", assignment.ResourceId.ToString()));
            }
        }

        var referenced = new HashSet<MatchId>(request.TargetMatchIds);
        foreach (var assignment in request.Existing.Assignments)
        {
            referenced.Add(assignment.MatchId);
        }

        foreach (var precedence in request.Precedences)
        {
            referenced.Add(precedence.PredecessorMatchId);
            referenced.Add(precedence.SuccessorMatchId);
            if (!matchIds.Contains(precedence.PredecessorMatchId)
                || !matchIds.Contains(precedence.SuccessorMatchId))
            {
                errors.Add(Err(
                    "Precedence references an unknown match context.",
                    precedence.PredecessorMatchId.ToString(),
                    precedence.SuccessorMatchId.ToString()));
            }
        }

        foreach (var sameStart in request.SameStarts)
        {
            referenced.Add(sameStart.MatchA);
            referenced.Add(sameStart.MatchB);
            if (!matchIds.Contains(sameStart.MatchA) || !matchIds.Contains(sameStart.MatchB))
            {
                errors.Add(Err("SameStart references an unknown match context.", sameStart.MatchA.ToString(), sameStart.MatchB.ToString()));
            }
        }

        foreach (var separation in request.MinimumSeparations)
        {
            referenced.Add(separation.MatchA);
            referenced.Add(separation.MatchB);
            if (!matchIds.Contains(separation.MatchA) || !matchIds.Contains(separation.MatchB))
            {
                errors.Add(Err("MinimumSeparation references an unknown match context.", separation.MatchA.ToString(), separation.MatchB.ToString()));
            }
        }

        errors.AddRange(from match in request.Matches where !referenced.Contains(match.MatchId) select Err("Match context is unused (ghost context).", match.MatchId.ToString()));

        // Fixed ∪ Targets: every constraint endpoint must be schedulable (assigned as Fixed or searched as Target).
        var schedulable = new HashSet<MatchId>(request.TargetMatchIds);
        foreach (var assignment in request.Existing.Assignments)
        {
            schedulable.Add(assignment.MatchId);
        }

        foreach (var precedence in request.Precedences)
        {
            if (!schedulable.Contains(precedence.PredecessorMatchId))
            {
                errors.Add(Err(
                    "Precedence predecessor must belong to Fixed ∪ Targets.",
                    precedence.PredecessorMatchId.ToString()));
            }

            if (!schedulable.Contains(precedence.SuccessorMatchId))
            {
                errors.Add(Err(
                    "Precedence successor must belong to Fixed ∪ Targets.",
                    precedence.SuccessorMatchId.ToString()));
            }
        }

        errors.AddRange(from sameStart in request.SameStarts where !schedulable.Contains(sameStart.MatchA) || !schedulable.Contains(sameStart.MatchB) select Err("SameStart endpoints must belong to Fixed ∪ Targets.", sameStart.MatchA.ToString(), sameStart.MatchB.ToString()));

        errors.AddRange(from separation in request.MinimumSeparations where !schedulable.Contains(separation.MatchA) || !schedulable.Contains(separation.MatchB) select Err("MinimumSeparation endpoints must belong to Fixed ∪ Targets.", separation.MatchA.ToString(), separation.MatchB.ToString()));

        if (HasPrecedenceCycle(request.Precedences))
        {
            errors.Add(Err("Precedence graph contains a cycle."));
        }

        return errors.Count == 0;
    }

    private static bool TryValidateFixed(
        SchedulingRequest request,
        IReadOnlyList<ScheduleAssignment> fixedAssignments,
        Dictionary<MatchId, MatchSchedulingContext> matchById,
        Dictionary<ResourceId, ResourceSchedulingContext> resourceById,
        List<SchedulingValidationError> errors)
    {
        foreach (var assignment in fixedAssignments)
        {
            var match = matchById[assignment.MatchId];
            var end = assignment.Start + match.Duration.AsTimeSpan();
            if (!request.Horizon.ContainsOccupation(assignment.Start, end))
            {
                errors.Add(Err("Fixed assignment is outside the horizon.", assignment.MatchId.ToString()));
            }

            if (!resourceById.TryGetValue(assignment.ResourceId, out var resource))
            {
                errors.Add(Err("Fixed assignment resource is unknown.", assignment.ResourceId.ToString()));
                continue;
            }

            if (!FitsAvailability(resource.AvailabilityWindows, assignment.Start, end))
            {
                errors.Add(Err("Fixed assignment is outside resource availability.", assignment.MatchId.ToString()));
            }
        }

        return errors.Count == 0;
    }

    private static bool TryValidateFixedConstraints(
        IReadOnlyList<ScheduleAssignment> fixedAssignments,
        Dictionary<MatchId, MatchSchedulingContext> matchById,
        MinimumGapBetweenMatches? gap,
        IReadOnlyList<Precedence> precedences,
        IReadOnlyList<MinimumSeparation> separations,
        Dictionary<MatchId, MatchId> sameStartParent,
        List<SchedulingValidationError> errors)
    {
        var occupations = fixedAssignments.ToDictionary(
            a => a.MatchId,
            a => new Occupation(a.ResourceId, a.Start, a.Start + matchById[a.MatchId].Duration.AsTimeSpan()));

        foreach (var group in occupations.Values.GroupBy(o => o.ResourceId))
        {
            var ordered = group.OrderBy(o => o.Start).ToArray();
            for (var i = 0; i < ordered.Length; i++)
            {
                for (var j = i + 1; j < ordered.Length; j++)
                {
                    if (Overlaps(ordered[i].Start, ordered[i].End, ordered[j].Start, ordered[j].End))
                    {
                        errors.Add(Err("Fixed assignments overlap on the same resource."));
                    }
                    else if (gap is not null
                             && !RespectsSeparation(
                                 ordered[i].Start,
                                 ordered[i].End,
                                 ordered[j].Start,
                                 ordered[j].End,
                                 gap.AsTimeSpan()))
                    {
                        errors.Add(Err("Fixed assignments violate minimum gap on the same resource."));
                    }
                }
            }
        }

        foreach (var precedence in precedences)
        {
            if (occupations.TryGetValue(precedence.PredecessorMatchId, out var pred)
                && occupations.TryGetValue(precedence.SuccessorMatchId, out var succ)
                && succ.Start < pred.End + precedence.MinimumDelay)
            {
                errors.Add(Err(
                    "Fixed assignments violate precedence.",
                    precedence.PredecessorMatchId.ToString(),
                    precedence.SuccessorMatchId.ToString()));
            }
        }

        foreach (var separation in separations)
        {
            if (occupations.TryGetValue(separation.MatchA, out var a)
                && occupations.TryGetValue(separation.MatchB, out var b)
                && !RespectsSeparation(a.Start, a.End, b.Start, b.End, separation.AsTimeSpan()))
            {
                errors.Add(Err(
                    "Fixed assignments violate minimum separation.",
                    separation.MatchA.ToString(),
                    separation.MatchB.ToString()));
            }
        }

        var startsByRoot = new Dictionary<MatchId, DateTimeOffset>();
        foreach (var assignment in fixedAssignments)
        {
            var root = FindRoot(sameStartParent, assignment.MatchId);
            if (startsByRoot.TryGetValue(root, out var start) && start != assignment.Start)
            {
                errors.Add(Err("Fixed SameStart starts conflict.", assignment.MatchId.ToString()));
            }
            else
            {
                startsByRoot[root] = assignment.Start;
            }
        }

        return errors.Count == 0;
    }

    private static bool AreWindowsStrictlyDisjointNonAdjacent(IReadOnlyList<TimeWindow> windows)
    {
        if (windows.Count <= 1)
        {
            return true;
        }

        var ordered = windows.OrderBy(w => w.Start).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            if (ordered[i].Start <= ordered[i - 1].End)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasPrecedenceCycle(IReadOnlyList<Precedence> precedences)
    {
        var adjacency = new Dictionary<MatchId, List<MatchId>>();
        foreach (var precedence in precedences)
        {
            if (!adjacency.TryGetValue(precedence.PredecessorMatchId, out var list))
            {
                list = [];
                adjacency[precedence.PredecessorMatchId] = list;
            }

            list.Add(precedence.SuccessorMatchId);
        }

        var visiting = new HashSet<MatchId>();
        var visited = new HashSet<MatchId>();

        return adjacency.Keys.Any(dfs);

        bool dfs(MatchId node)
        {
            if (visiting.Contains(node))
            {
                return true;
            }

            if (!visited.Add(node))
            {
                return false;
            }

            visiting.Add(node);
            if (adjacency.TryGetValue(node, out var next))
            {
                if (next.Any(dfs))
                {
                    return true;
                }
            }

            visiting.Remove(node);
            return false;
        }
    }

    private static Dictionary<MatchId, MatchId> BuildSameStartParents(
        IReadOnlyList<SameStart> sameStarts,
        IEnumerable<MatchId> allMatchIds)
    {
        var parent = allMatchIds.ToDictionary(id => id, id => id);
        foreach (var sameStart in sameStarts)
        {
            Union(parent, sameStart.MatchA, sameStart.MatchB);
        }

        return parent;
    }

    private static void Union(Dictionary<MatchId, MatchId> parent, MatchId a, MatchId b)
    {
        var rootA = FindRoot(parent, a);
        var rootB = FindRoot(parent, b);
        if (!rootA.Equals(rootB))
        {
            parent[rootB] = rootA;
        }
    }

    private static MatchId FindRoot(Dictionary<MatchId, MatchId> parent, MatchId id)
    {
        var root = id;
        while (!parent[root].Equals(root))
        {
            root = parent[root];
        }

        var current = id;
        while (!current.Equals(root))
        {
            var next = parent[current];
            parent[current] = root;
            current = next;
        }

        return root;
    }

    private static Precedence[] NormalizePrecedences(IReadOnlyList<Precedence> precedences)
    {
        var map = new Dictionary<(MatchId, MatchId), int>();
        foreach (var precedence in precedences)
        {
            var key = (precedence.PredecessorMatchId, precedence.SuccessorMatchId);
            map[key] = map.TryGetValue(key, out var existing)
                ? Math.Max(existing, precedence.MinimumDelayMinutes)
                : precedence.MinimumDelayMinutes;
        }

        return [.. map.Select(pair => new Precedence(pair.Key.Item1, pair.Key.Item2, pair.Value))];
    }

    private static MinimumSeparation[] NormalizeSeparations(IReadOnlyList<MinimumSeparation> separations)
    {
        var map = new Dictionary<(MatchId, MatchId), int>();
        foreach (var separation in separations)
        {
            var a = separation.MatchA;
            var b = separation.MatchB;
            var key = a.Value.CompareTo(b.Value) <= 0 ? (a, b) : (b, a);
            map[key] = map.TryGetValue(key, out var existing)
                ? Math.Max(existing, separation.Minutes)
                : separation.Minutes;
        }

        return [.. map.Select(pair => new MinimumSeparation(pair.Key.Item1, pair.Key.Item2, pair.Value))];
    }

    private static SchedulingValidationError Err(string message, params string[] subjects) =>
        new(SchedulingErrorCodes.InvalidRequest, message, subjects);

    private readonly record struct Candidate(ResourceId ResourceId, DateTimeOffset Start);

    private readonly record struct Occupation(ResourceId ResourceId, DateTimeOffset Start, DateTimeOffset End);

    private sealed class SearchState
    {
        private readonly Dictionary<MatchId, Occupation> _occupations = [];
        private readonly List<Effect> _undo = [];
        private readonly Dictionary<MatchId, MatchSchedulingContext> _matchById;

        private SearchState(
            Dictionary<MatchId, MatchId> sameStartParent,
            Dictionary<MatchId, DateTimeOffset?> componentBound,
            Dictionary<MatchId, MatchSchedulingContext> matchById)
        {
            SameStartParent = sameStartParent;
            ComponentBound = componentBound;
            _matchById = matchById;
        }

        public Dictionary<MatchId, MatchId> SameStartParent { get; }

        public Dictionary<MatchId, DateTimeOffset?> ComponentBound { get; }

        public static SearchState Create(
            IReadOnlyList<ScheduleAssignment> fixedAssignments,
            Dictionary<MatchId, MatchSchedulingContext> matchById,
            Dictionary<MatchId, DateTimeOffset?> componentBound,
            Dictionary<MatchId, MatchId> sameStartParent)
        {
            // Copy ctor — collection expression cannot spread IDictionary into Dictionary.
#pragma warning disable IDE0028
            var state = new SearchState(sameStartParent, new Dictionary<MatchId, DateTimeOffset?>(componentBound), matchById);
#pragma warning restore IDE0028
            foreach (var assignment in fixedAssignments)
            {
                var end = assignment.Start + matchById[assignment.MatchId].Duration.AsTimeSpan();
                state._occupations[assignment.MatchId] = new Occupation(assignment.ResourceId, assignment.Start, end);
            }

            return state;
        }

        public IEnumerable<Occupation> OccupationsOn(ResourceId resourceId) =>
            _occupations.Values.Where(o => o.ResourceId.Equals(resourceId));

        public bool TryGetOccupation(MatchId matchId, out Occupation occupation) =>
            _occupations.TryGetValue(matchId, out occupation);

        public void Apply(MatchId matchId, Candidate candidate, MatchId root, bool setBound)
        {
            var end = candidate.Start + _matchById[matchId].Duration.AsTimeSpan();
            _occupations[matchId] = new Occupation(candidate.ResourceId, candidate.Start, end);
            DateTimeOffset? previousBound = null;
            var boundChanged = false;
            if (setBound)
            {
                previousBound = ComponentBound[root];
                ComponentBound[root] = candidate.Start;
                boundChanged = true;
            }

            _undo.Add(new Effect(matchId, root, boundChanged, previousBound));
        }

        public void Undo()
        {
            var effect = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            _occupations.Remove(effect.MatchId);
            if (effect.BoundChanged)
            {
                ComponentBound[effect.Root] = effect.PreviousBound;
            }
        }

        public Schedule ToSchedule()
        {
            var assignments = _occupations
                .Select(pair => new ScheduleAssignment(pair.Key, pair.Value.ResourceId, pair.Value.Start))
                .OrderBy(a => a.MatchId.Value)
                .ToArray();
            return new Schedule(assignments);
        }

        private readonly record struct Effect(
            MatchId MatchId,
            MatchId Root,
            bool BoundChanged,
            DateTimeOffset? PreviousBound);
    }
}
