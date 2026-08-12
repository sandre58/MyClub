// -----------------------------------------------------------------------
// <copyright file="DrawResolutionGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyNet.Generator;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Pure Domain service: proposes one admissible <see cref="DrawResolution"/> under Required constraints,
/// or <see cref="DrawGenerationResult.NoSolution"/>. Does not mutate Draw / Stage.
/// V1: Slot and Pairing only. Preferred ignored. Invalid request → <see cref="DomainException"/>.
/// SameGroupAvoidance / SameTeamAvoidance Required apply to Pairing only; on Slot they are Invalid
/// (Host should filter Stage DrawRules by kind when assembling the request).
/// </summary>
public static class DrawResolutionGenerator
{
    /// <summary>
    /// Generates a resolution from a flat request (never a Draw aggregate).
    /// </summary>
    /// <param name="request">Generation request assembled by Application.</param>
    /// <returns>Resolved or NoSolution.</returns>
    public static DrawGenerationResult Generate(DrawGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        return request.Kind switch
        {
            DrawResolutionKind.Slot => GenerateSlot(request),
            DrawResolutionKind.Pairing => GeneratePairing(request),
            DrawResolutionKind.Group => throw Invalid("Draw kind is not supported by the V1 generator."),
            _ => throw Invalid("Draw kind is not supported by the V1 generator.")
        };
    }

    private static void ValidateRequest(DrawGenerationRequest request)
    {
        if (!Enum.IsDefined(request.Kind))
        {
            throw Invalid("Draw resolution kind is unknown.");
        }

        if (request.Kind is not (DrawResolutionKind.Slot or DrawResolutionKind.Pairing))
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

        var required = RequiredConstraints(request.Constraints);
        EnsureConstraintMaps(request.Kind, required, request.Entries, request.ConstraintContext);

        switch (request.Kind)
        {
            case DrawResolutionKind.Slot:
                ValidateSlotShape(request);
                break;
            case DrawResolutionKind.Pairing:
                ValidatePairingShape(request);
                break;
            case DrawResolutionKind.Group:
            default:
                break;
        }
    }

    private static void ValidateSlotShape(DrawGenerationRequest request)
    {
        if (request.Targets is null || request.Targets.Count == 0)
        {
            throw Invalid("Slot generation requires target slot keys.");
        }

        if (request.FixedPairings.Count > 0)
        {
            throw Invalid("Slot generation cannot include fixed pairings.");
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

    private static void ValidatePairingShape(DrawGenerationRequest request)
    {
        if (request.Targets is { Count: > 0 })
        {
            throw Invalid("Pairing generation does not accept slot targets.");
        }

        if (request.FixedSlots.Count > 0)
        {
            throw Invalid("Pairing generation cannot include fixed slot placements.");
        }

        if (request.Entries.Count % 2 != 0)
        {
            throw Invalid("Pairing generation requires an even entry pool.");
        }

        var used = new HashSet<EntryId>();
        foreach (var fixedPairing in request.FixedPairings)
        {
            if (!request.Entries.Contains(fixedPairing.EntryA) || !request.Entries.Contains(fixedPairing.EntryB))
            {
                throw Invalid("Fixed pairing references an entry outside the pool.");
            }

            if (!used.Add(fixedPairing.EntryA) || !used.Add(fixedPairing.EntryB))
            {
                throw Invalid("Fixed pairings reuse an entry.");
            }
        }
    }

    private static void EnsureConstraintMaps(
        DrawResolutionKind kind,
        IReadOnlyList<DrawConstraint> required,
        IReadOnlyList<EntryId> entries,
        DrawConstraintContext context)
    {
        foreach (var constraint in required)
        {
            switch (constraint.ConstraintType)
            {
                case DrawConstraintType.SameAssociationAvoidance:
                    throw Invalid("SameAssociationAvoidance is not supported in V1.");
                case DrawConstraintType.SameGroupAvoidance:
                    if (kind == DrawResolutionKind.Slot)
                    {
                        throw Invalid("SameGroupAvoidance Required is not supported for Slot generation in V1.");
                    }

                    EnsureCompleteMap(entries, context.SourceGroupMap, "SameGroupAvoidance");
                    break;
                case DrawConstraintType.SameTeamAvoidance:
                    if (kind == DrawResolutionKind.Slot)
                    {
                        throw Invalid("SameTeamAvoidance Required is not supported for Slot generation in V1.");
                    }

                    EnsureCompleteMap(entries, context.TeamMap, "SameTeamAvoidance");
                    break;
                default:
                    throw Invalid($"Draw constraint type '{constraint.ConstraintType}' is unknown.");
            }
        }
    }

    private static void EnsureCompleteMap<T>(
        IReadOnlyList<EntryId> entries,
        IReadOnlyDictionary<EntryId, T>? map,
        string constraintName)
    {
        if (map is null)
        {
            throw Invalid($"{constraintName} Required requires a complete constraint map.");
        }

        if (entries.Any(entryId => !map.ContainsKey(entryId)))
        {
            throw Invalid($"{constraintName} Required map is incomplete for the entry pool.");
        }
    }

    private static DrawGenerationResult GenerateSlot(DrawGenerationRequest request)
    {
        var targets = NormalizeTargets(request.Targets!);
        var fixedBySlot = request.FixedSlots.ToDictionary(f => f.SlotKey, f => f.EntryId, StringComparer.Ordinal);
        var fixedEntries = request.FixedSlots.Select(f => f.EntryId).ToHashSet();
        var freeTargets = targets.Where(t => !fixedBySlot.ContainsKey(t)).ToList();
        var freeEntries = Shuffle(request.Entries.Where(e => !fixedEntries.Contains(e)).ToList(), request.RandomSource);

        // Exhaustive permutations via backtracking (randomized candidate order).
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
        var candidates = Shuffle(freeEntries.Where(e => !assignment.ContainsValue(e)).ToList(), random);
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

    private static DrawGenerationResult GeneratePairing(DrawGenerationRequest request)
    {
        var required = RequiredConstraints(request.Constraints);
        var used = new HashSet<EntryId>();
        var pairings = new List<PairingDrawResult>();

        foreach (var fixedPairing in request.FixedPairings)
        {
            if (!IsPairAllowed(fixedPairing.EntryA, fixedPairing.EntryB, required, request.ConstraintContext))
            {
                // Fixed already violate Required → no admissible completion.
                return DrawGenerationResult.NoSolution();
            }

            pairings.Add(fixedPairing);
            used.Add(fixedPairing.EntryA);
            used.Add(fixedPairing.EntryB);
        }

        var remaining = Shuffle(request.Entries.Where(e => !used.Contains(e)).ToList(), request.RandomSource);
        return !TryPair(remaining, pairings, required, request.ConstraintContext, request.RandomSource)
            ? DrawGenerationResult.NoSolution()
            : DrawGenerationResult.Resolved(DrawResolution.ResolvedPairings(pairings));
    }

    private static bool TryPair(
        List<EntryId> remaining,
        List<PairingDrawResult> pairings,
        IReadOnlyList<DrawConstraint> required,
        DrawConstraintContext context,
        IRandomSource random)
    {
        if (remaining.Count == 0)
        {
            return true;
        }

        var left = remaining[0];
        var partners = Shuffle(remaining.Skip(1).ToList(), random);
        foreach (var right in partners.Where(right => IsPairAllowed(left, right, required, context)))
        {
            pairings.Add(new PairingDrawResult(left, right));
            var next = remaining.Where(e => !e.Equals(left) && !e.Equals(right)).ToList();
            if (TryPair(next, pairings, required, context, random))
            {
                return true;
            }

            pairings.RemoveAt(pairings.Count - 1);
        }

        return false;
    }

    private static bool IsPairAllowed(
        EntryId a,
        EntryId b,
        IReadOnlyList<DrawConstraint> required,
        DrawConstraintContext context)
    {
        if (a.Equals(b))
        {
            return false;
        }

        foreach (var constraint in required)
        {
            switch (constraint.ConstraintType)
            {
                case DrawConstraintType.SameGroupAvoidance:
                    if (context.SourceGroupMap![a].Equals(context.SourceGroupMap[b]))
                    {
                        return false;
                    }

                    break;
                case DrawConstraintType.SameTeamAvoidance:
                    if (context.TeamMap![a].Equals(context.TeamMap[b]))
                    {
                        return false;
                    }

                    break;
                case DrawConstraintType.SameAssociationAvoidance:
                default:
                    break;
            }
        }

        return true;
    }

    private static IReadOnlyList<DrawConstraint> RequiredConstraints(IReadOnlyList<DrawConstraint> constraints) =>
        [..constraints.Where(c => c.Enforcement == ConstraintEnforcement.Required)];

    private static List<string> NormalizeTargets(IReadOnlyList<string> targets) =>
        [..targets.Select(Slot.NormalizeKey)];

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
