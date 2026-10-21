// -----------------------------------------------------------------------
// <copyright file="DrawResolutionGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Pure Domain service: proposes one admissible <see cref="DrawResolution"/> under Required constraints,
/// or <see cref="DrawGenerationResult.NoSolution"/>.
/// Does not mutate Draw / Stage. Slot and Group (pots). Invalid request → <see cref="DomainException"/>.
/// Group: uniform capacity, mandatory pots, ≤1 entry per pot per group;
/// optional Required <c>MaxSameAssociationPerGroup</c>. Soft Group constraints are out of scope.
/// Without MaxSameAssociation, G3–G7 alone always resolve; with MaxSameAssociation, NoSolution is a real outcome.
/// </summary>
public static class DrawResolutionGenerator
{
    /// <summary>
    /// Generates a resolution from a flat request (never a Draw aggregate).
    /// </summary>
    /// <param name="request">Generation request assembled by Application.</param>
    /// <returns>Resolved (with optional Preferred violations) or NoSolution.</returns>
    public static DrawGenerationResult Generate(DrawGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        return request.Kind switch
        {
            DrawResolutionKind.Slot => GenerateSlot(request),
            DrawResolutionKind.Group => GenerateGroup(request),
            _ => throw Invalid("Draw kind is not supported by the V1 generator.")
        };
    }

    private static void ValidateRequest(DrawGenerationRequest request)
    {
        if (!Enum.IsDefined(request.Kind))
        {
            throw Invalid("Draw resolution kind is unknown.");
        }

        if (request.Kind is not (DrawResolutionKind.Slot or DrawResolutionKind.Group))
        {
            throw Invalid($"Draw kind '{request.Kind}' is not supported by the V1 generator.");
        }

        if (request.Entries.Count == 0)
        {
            throw Invalid("Generation requires at least one entry.");
        }

        if (request.Entries.Distinct().Count() != request.Entries.Count)
        {
            throw Invalid("Entry pool cannot contain duplicates.");
        }

        if (request.Kind != DrawResolutionKind.Group)
        {
            EnsureConstraintMaps(request.Kind, request.Constraints, request.Entries, request.ConstraintContext);
        }

        switch (request.Kind)
        {
            case DrawResolutionKind.Slot:
                ValidateSlotShape(request);
                break;
            case DrawResolutionKind.Group:
                ValidateGroupShape(request);
                ValidateGroupConstraints(request);
                break;
            default:
                break;
        }
    }

    private static void ValidateSlotShape(DrawGenerationRequest request)
    {
        EnsureNoGroupFields(request, "Slot");

        if (request.Targets is null || request.Targets.Count == 0)
        {
            throw Invalid("Slot generation requires target slot keys.");
        }

        var targets = NormalizeTargets(request.Targets);
        if (targets.Count != request.Targets.Count
            || targets.Distinct(StringComparer.Ordinal).Count() != targets.Count)
        {
            throw Invalid("Slot targets must be unique non-empty keys.");
        }

        if (request.FixedSlots.Select(f => f.EntryId).Distinct().Count() != request.FixedSlots.Count
            || request.FixedSlots.Select(f => f.SlotKey).Distinct(StringComparer.Ordinal).Count() != request.FixedSlots.Count)
        {
            throw Invalid("Fixed slot placements must have unique entries and unique slot keys.");
        }

        foreach (var fixedPlacement in request.FixedSlots)
        {
            if (!request.Entries.Contains(fixedPlacement.EntryId))
            {
                throw Invalid("Fixed slot placement references an entry outside the pool.");
            }

            if (!targets.Contains(fixedPlacement.SlotKey, StringComparer.Ordinal))
            {
                throw Invalid("Fixed slot placement references a slot key outside the targets.");
            }
        }

        var freeEntries = request.Entries.Count - request.FixedSlots.Count;
        var freeTargets = targets.Count - request.FixedSlots.Count;
        if (freeEntries != freeTargets)
        {
            throw Invalid("Slot coverage is inconsistent: free entries must equal free targets.");
        }

        if (targets.Count != request.Entries.Count)
        {
            throw Invalid("Slot targets count must equal the entry pool size.");
        }
    }

    private static void ValidateGroupShape(DrawGenerationRequest request)
    {
        if (request.Targets is { Count: > 0 })
        {
            throw Invalid("Group generation does not accept slot targets.");
        }

        if (request.FixedSlots.Count > 0)
        {
            throw Invalid("Group generation cannot include fixed slot placements.");
        }

        if (request.GroupTargets is null || request.GroupTargets.Count == 0)
        {
            throw Invalid("Group generation requires target group identities.");
        }

        if (request.GroupTargets.Distinct().Count() != request.GroupTargets.Count)
        {
            throw Invalid("Group targets must be unique.");
        }

        if (request.Entries.Count % request.GroupTargets.Count != 0)
        {
            throw Invalid("Group capacity requires an exact division of entries by group targets.");
        }

        var capacity = request.Entries.Count / request.GroupTargets.Count;
        if (request.NumberOfPots is null or < 2)
        {
            throw Invalid("Group generation requires PotRules.NumberOfPots (≥ 2).");
        }

        var numberOfPots = request.NumberOfPots.Value;
        if (numberOfPots != capacity)
        {
            throw Invalid("Group V1 requires NumberOfPots to equal uniform group capacity.");
        }

        if (request.PotMembership is null)
        {
            throw Invalid("Group generation requires PotMembership.");
        }

        var pots = request.PotMembership.Pots;
        foreach (var entryId in request.Entries)
        {
            if (!pots.TryGetValue(entryId, out var pot))
            {
                throw Invalid("PotMembership is incomplete for the entry pool.");
            }

            if (pot < 1 || pot > numberOfPots)
            {
                throw Invalid("PotMembership contains a pot number outside [1..NumberOfPots].");
            }
        }

        if (pots.Keys.Any(entryId => !request.Entries.Contains(entryId)))
        {
            throw Invalid("PotMembership contains an entry outside the pool.");
        }

        for (var pot = 1; pot <= numberOfPots; pot++)
        {
            var potSize = pots.Count(pair => pair.Value == pot);
            if (potSize != request.GroupTargets.Count)
            {
                throw Invalid("Each pot must contain exactly as many entries as there are group targets.");
            }
        }

        if (request.FixedGroups.Select(f => f.EntryId).Distinct().Count() != request.FixedGroups.Count)
        {
            throw Invalid("Fixed group placements must have unique entries.");
        }

        var occupiedPotsByGroup = request.GroupTargets.ToDictionary(g => g, _ => new HashSet<int>());
        var countByGroup = request.GroupTargets.ToDictionary(g => g, _ => 0);

        foreach (var fixedPlacement in request.FixedGroups)
        {
            if (!request.Entries.Contains(fixedPlacement.EntryId))
            {
                throw Invalid("Fixed group placement references an entry outside the pool.");
            }

            if (!countByGroup.TryGetValue(fixedPlacement.GroupId, out var groupCount))
            {
                throw Invalid("Fixed group placement references a group outside the targets.");
            }

            var pot = pots[fixedPlacement.EntryId];
            if (!occupiedPotsByGroup[fixedPlacement.GroupId].Add(pot))
            {
                throw Invalid("Fixed group placements put two entries from the same pot into one group.");
            }

            groupCount++;
            countByGroup[fixedPlacement.GroupId] = groupCount;
            if (groupCount > capacity)
            {
                throw Invalid("Fixed group placements exceed group capacity.");
            }
        }
    }

    private static void EnsureNoGroupFields(DrawGenerationRequest request, string kindLabel)
    {
        if (request.GroupTargets is { Count: > 0 })
        {
            throw Invalid($"{kindLabel} generation does not accept group targets.");
        }

        if (request.FixedGroups.Count > 0)
        {
            throw Invalid($"{kindLabel} generation cannot include fixed group placements.");
        }

        if (request.PotMembership is not null || request.NumberOfPots is not null)
        {
            throw Invalid($"{kindLabel} generation does not accept pot inputs.");
        }
    }

    private static void EnsureConstraintMaps(
        DrawResolutionKind kind,
        IReadOnlyList<DrawConstraint> constraints,
        IReadOnlyList<EntryId> entries,
        DrawConstraintContext context)
    {
        _ = kind;
        _ = entries;
        _ = context;

        foreach (var constraint in constraints)
        {
            throw constraint.ConstraintType switch
            {
                DrawConstraintType.SameAssociationAvoidance => Invalid(
                    "SameAssociationAvoidance is not supported in V1."),
                DrawConstraintType.SameGroupAvoidance => Invalid(
                    "SameGroupAvoidance is not supported for Slot generation in V1."),
                DrawConstraintType.SameTeamAvoidance => Invalid(
                    "SameTeamAvoidance is not supported for Slot generation in V1."),
                DrawConstraintType.MaxSameAssociationPerGroup => Invalid(
                    "MaxSameAssociationPerGroup is only supported for Group generation."),
                _ => Invalid($"Draw constraint type '{constraint.ConstraintType}' is unknown.")
            };
        }
    }

    private static void ValidateGroupConstraints(DrawGenerationRequest request)
    {
        if (request.Constraints.Any(constraint => constraint.ConstraintType != DrawConstraintType.MaxSameAssociationPerGroup))
        {
            throw Invalid("Group generation accepts only MaxSameAssociationPerGroup constraints in V1.");
        }

        var maxConstraints = request.Constraints
            .Where(c => c.ConstraintType == DrawConstraintType.MaxSameAssociationPerGroup)
            .ToArray();
        switch (maxConstraints.Length)
        {
            case > 1:
                throw Invalid("Group generation accepts at most one MaxSameAssociationPerGroup constraint.");
            case 0:
                return;
            default:
                EnsureCompleteMap(
                    request.Entries,
                    request.ConstraintContext.AssociationMap,
                    "MaxSameAssociationPerGroup");
                break;
        }
    }

    private static int? ResolveMaxSameAssociation(IReadOnlyList<DrawConstraint> constraints)
    {
        var values = constraints
            .Where(c => c.ConstraintType == DrawConstraintType.MaxSameAssociationPerGroup)
            .Select(c => c.MaxPerGroup!.Value)
            .ToArray();

        return values.Length == 0 ? null : values[0];
    }

    private static void EnsureCompleteMap<T>(
        IReadOnlyList<EntryId> entries,
        IReadOnlyDictionary<EntryId, T>? map,
        string constraintName)
    {
        if (map is null)
        {
            throw Invalid($"{constraintName} requires a complete constraint map.");
        }

        if (entries.Any(entryId => !map.ContainsKey(entryId)))
        {
            throw Invalid($"{constraintName} map is incomplete for the entry pool.");
        }
    }

    private static DrawGenerationResult GenerateSlot(DrawGenerationRequest request)
    {
        var targets = NormalizeTargets(request.Targets!);
        var fixedBySlot = request.FixedSlots.ToDictionary(f => f.SlotKey, f => f.EntryId, StringComparer.Ordinal);
        var fixedEntries = request.FixedSlots.Select(f => f.EntryId).ToHashSet();
        var freeTargets = targets.Where(t => !fixedBySlot.ContainsKey(t)).ToList();
        var freeEntries = Shuffle([.. request.Entries.Where(e => !fixedEntries.Contains(e))], request.RandomSource);

        var assignment = new Dictionary<string, EntryId>(StringComparer.Ordinal);
        foreach (var fixedPlacement in request.FixedSlots)
        {
            assignment[fixedPlacement.SlotKey] = fixedPlacement.EntryId;
        }

        if (!TryAssignSlots(freeTargets, freeEntries, 0, assignment, request.RandomSource))
        {
            return DrawGenerationResult.NoSolution();
        }

        var results = targets.ConvertAll(key => new SlotDrawPlacement(assignment[key], key));
        return DrawGenerationResult.Resolved(DrawResolution.ResolvedSlots(results));
    }

    private static bool TryAssignSlots(
        IReadOnlyList<string> freeTargets,
        IReadOnlyList<EntryId> freeEntries,
        int index,
        Dictionary<string, EntryId> assignment,
        IRandomSource random)
    {
        if (index >= freeTargets.Count)
        {
            return true;
        }

        var target = freeTargets[index];
        var candidates = Shuffle([.. freeEntries.Where(e => !assignment.ContainsValue(e))], random);
        foreach (var entry in candidates)
        {
            assignment[target] = entry;
            if (TryAssignSlots(freeTargets, freeEntries, index + 1, assignment, random))
            {
                return true;
            }

            assignment.Remove(target);
        }

        return false;
    }

    private static DrawGenerationResult GenerateGroup(DrawGenerationRequest request)
    {
        // Without MaxSameAssociationPerGroup (G3–G7 alone): always structurally resolvable.
        // With MaxSameAssociationPerGroup: NoSolution is a real business outcome when Required-feasible
        // placements do not exist (do not pre-classify that case as Invalid).
        var capacity = request.Entries.Count / request.GroupTargets!.Count;
        var pots = request.PotMembership!.Pots;
        var maxSameAssociation = ResolveMaxSameAssociation(request.Constraints);
        var associationMap = request.ConstraintContext.AssociationMap;
        var placements = new List<GroupDrawPlacement>();
        var occupiedPots = request.GroupTargets.ToDictionary(g => g, _ => new HashSet<int>());
        var counts = request.GroupTargets.ToDictionary(g => g, _ => 0);
        var associationCounts = request.GroupTargets.ToDictionary(
            g => g,
            _ => new Dictionary<AssociationId, int>());
        var usedEntries = new HashSet<EntryId>();

        foreach (var fixedPlacement in request.FixedGroups)
        {
            placements.Add(fixedPlacement);
            usedEntries.Add(fixedPlacement.EntryId);
            occupiedPots[fixedPlacement.GroupId].Add(pots[fixedPlacement.EntryId]);
            counts[fixedPlacement.GroupId]++;
            if (associationMap is null || maxSameAssociation is null) continue;
            var associationId = associationMap[fixedPlacement.EntryId];
            IncrementAssociation(associationCounts[fixedPlacement.GroupId], associationId);
            if (associationCounts[fixedPlacement.GroupId][associationId] > maxSameAssociation.Value)
            {
                // Fixed are structurally valid but already incompatible with Required Max → NoSolution.
                return DrawGenerationResult.NoSolution();
            }
        }

        var remaining = Shuffle(
            [.. request.Entries.Where(e => !usedEntries.Contains(e))],
            request.RandomSource);

        return !TryAssignGroups(
                remaining,
                placements,
                occupiedPots,
                counts,
                associationCounts,
                pots,
                associationMap,
                maxSameAssociation,
                capacity,
                request.GroupTargets,
                request.RandomSource)
            ? DrawGenerationResult.NoSolution()
            : DrawGenerationResult.Resolved(DrawResolution.ResolvedGroups(placements));
    }

    private static bool TryAssignGroups(
        List<EntryId> remaining,
        List<GroupDrawPlacement> placements,
        Dictionary<GroupId, HashSet<int>> occupiedPots,
        Dictionary<GroupId, int> counts,
        Dictionary<GroupId, Dictionary<AssociationId, int>> associationCounts,
        IReadOnlyDictionary<EntryId, int> pots,
        IReadOnlyDictionary<EntryId, AssociationId>? associationMap,
        int? maxSameAssociation,
        int capacity,
        IReadOnlyList<GroupId> groupTargets,
        IRandomSource random)
    {
        if (remaining.Count == 0)
        {
            return true;
        }

        var entry = remaining[0];
        var pot = pots[entry];
        var associationId = associationMap?[entry];
        var candidates = Shuffle(
            [
                .. groupTargets
                    .Where(g =>
                        counts[g] < capacity
                        && !occupiedPots[g].Contains(pot)
                        && IsAssociationAllowed(associationCounts[g], associationId, maxSameAssociation))
            ],
            random);

        foreach (var groupId in candidates)
        {
            placements.Add(new GroupDrawPlacement(entry, groupId));
            occupiedPots[groupId].Add(pot);
            counts[groupId]++;
            if (associationId is { } assoc)
            {
                IncrementAssociation(associationCounts[groupId], assoc);
            }

            var next = remaining.Skip(1).ToList();
            if (TryAssignGroups(
                    next,
                    placements,
                    occupiedPots,
                    counts,
                    associationCounts,
                    pots,
                    associationMap,
                    maxSameAssociation,
                    capacity,
                    groupTargets,
                    random))
            {
                return true;
            }

            counts[groupId]--;
            occupiedPots[groupId].Remove(pot);
            if (associationId is { } assocToRemove)
            {
                DecrementAssociation(associationCounts[groupId], assocToRemove);
            }

            placements.RemoveAt(placements.Count - 1);
        }

        return false;
    }

    private static bool IsAssociationAllowed(
        Dictionary<AssociationId, int> groupAssociationCounts,
        AssociationId? associationId,
        int? maxSameAssociation)
    {
        if (maxSameAssociation is null || associationId is null)
        {
            return true;
        }

        groupAssociationCounts.TryGetValue(associationId.Value, out var count);
        return count < maxSameAssociation.Value;
    }

    private static void IncrementAssociation(Dictionary<AssociationId, int> counts, AssociationId associationId)
    {
        counts.TryGetValue(associationId, out var count);
        counts[associationId] = count + 1;
    }

    private static void DecrementAssociation(Dictionary<AssociationId, int> counts, AssociationId associationId)
    {
        if (--counts[associationId] == 0)
        {
            counts.Remove(associationId);
        }
    }

    private static List<string> NormalizeTargets(IReadOnlyList<string> targets) =>
        [.. targets.Select(Slot.NormalizeKey)];

    private static List<T> Shuffle<T>(List<T> items, IRandomSource random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.NextInt32(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }

    private static DomainException Invalid(string message) =>
        new(message, StageErrorCodes.DrawGenerationInvalid);
}
