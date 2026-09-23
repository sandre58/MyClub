// -----------------------------------------------------------------------
// <copyright file="DrawResolutionGeneratorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyNet.Generator;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class DrawResolutionGeneratorTests
{
    [Fact]
    public void RC1_slot_libre_produces_bijection()
    {
        var entries = NewEntries(4);
        var targets = new[] { "QF1", "QF2", "QF3", "QF4" };
        var request = SlotRequest(entries, targets, fixedSlots: [], constraints: [], seed: 42);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        var slots = result.Resolution!.SlotResults;
        slots.Should().HaveCount(4);
        slots.Select(s => s.EntryId).Should().BeEquivalentTo(entries);
        slots.Select(s => s.SlotKey).Should().BeEquivalentTo(targets);
    }

    [Fact]
    public void RC2_partial_fixed_preserves_fixed_and_fills_rest()
    {
        var entries = NewEntries(6);
        var targets = new[] { "S1", "S2", "S3", "S4", "S5", "S6" };
        var fixedSlots = new[]
        {
            new SlotDrawPlacement(entries[0], "S1"),
            new SlotDrawPlacement(entries[1], "S2")
        };
        var request = SlotRequest(entries, targets, fixedSlots, [], seed: 7);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        var slots = result.Resolution!.SlotResults;
        slots.Should().Contain(s => s.EntryId.Equals(entries[0]) && s.SlotKey == "S1");
        slots.Should().Contain(s => s.EntryId.Equals(entries[1]) && s.SlotKey == "S2");
        slots.Should().HaveCount(6);
        slots.Select(s => s.EntryId).Should().BeEquivalentTo(entries);
    }

    [Fact]
    public void RC7_same_seed_is_reproducible()
    {
        var entries = NewEntries(6);
        var targets = new[] { "A", "B", "C", "D", "E", "F" };
        var r1 = DrawResolutionGenerator.Generate(SlotRequest(entries, targets, [], [], seed: 99));
        var r2 = DrawResolutionGenerator.Generate(SlotRequest(entries, targets, [], [], seed: 99));

        r1.IsResolved.Should().BeTrue();
        r2.IsResolved.Should().BeTrue();
        r1.Resolution!.SlotResults.Select(s => (s.SlotKey, s.EntryId))
            .Should().Equal(r2.Resolution!.SlotResults.Select(s => (s.SlotKey, s.EntryId)));
    }

    [Fact]
    public void Group_RC1_canonical_16_4_4_places_one_per_pot_per_group()
    {
        var entries = NewEntries(16);
        var groups = NewGroups(4);
        var pots = BalancedPots(entries, numberOfPots: 4);
        var request = GroupRequest(entries, groups, pots, numberOfPots: 4, fixedGroups: [], seed: 21);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        result.PreferredViolations.Should().BeEmpty();
        AssertGroupResolution(result.Resolution!.GroupResults, entries, groups, pots, capacity: 4);
    }

    [Fact]
    public void Group_RC2_reduced_9_3_3_resolves()
    {
        var entries = NewEntries(9);
        var groups = NewGroups(3);
        var pots = BalancedPots(entries, numberOfPots: 3);
        var request = GroupRequest(entries, groups, pots, 3, [], seed: 9);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        AssertGroupResolution(result.Resolution!.GroupResults, entries, groups, pots, capacity: 3);
    }

    [Fact]
    public void Group_RC3_non_exact_division_is_invalid()
    {
        var entries = NewEntries(10);
        var groups = NewGroups(4);
        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, BalancedPots(entries, 2), 2, [], seed: 1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC4_missing_pot_membership_is_invalid()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var request = new DrawGenerationRequest(
            DrawResolutionKind.Group,
            entries,
            [],
            DrawConstraintContext.Empty,
            new SeededSource(1),
            groupTargets: groups,
            numberOfPots: 2,
            potMembership: null);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC5_number_of_pots_not_equal_capacity_is_invalid()
    {
        var entries = NewEntries(16);
        var groups = NewGroups(4);

        // Capacity would be 4; NumberOfPots 5 is structurally invalid for V1.
        var pots = new Dictionary<EntryId, int>();
        for (var i = 0; i < entries.Length; i++)
        {
            pots[entries[i]] = (i % 5) + 1;
        }

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, numberOfPots: 5, [], seed: 1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC6_unbalanced_pot_sizes_are_invalid()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = new Dictionary<EntryId, int>
        {
            [entries[0]] = 1, [entries[1]] = 1, [entries[2]] = 1, // pot1 size 3
            [entries[3]] = 2, [entries[4]] = 2,
            [entries[5]] = 3, [entries[6]] = 3,
            [entries[7]] = 4
        };

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, numberOfPots: 4, [], seed: 1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC7_compatible_fixed_are_respected()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, numberOfPots: 4);

        // Ensure pots of fixed entries differ.
        pots[entries[0]] = 1;
        pots[entries[1]] = 1;
        pots[entries[2]] = 2;
        pots[entries[3]] = 2;
        pots[entries[4]] = 3;
        pots[entries[5]] = 3;
        pots[entries[6]] = 4;
        pots[entries[7]] = 4;
        var fixedGroups = new[]
        {
            new GroupDrawPlacement(entries[0], groups[0]),
            new GroupDrawPlacement(entries[2], groups[0])
        };

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, 4, fixedGroups, seed: 12));

        result.IsResolved.Should().BeTrue();
        result.Resolution!.GroupResults.Should().Contain(p => p.EntryId.Equals(entries[0]) && p.GroupId.Equals(groups[0]));
        result.Resolution.GroupResults.Should().Contain(p => p.EntryId.Equals(entries[2]) && p.GroupId.Equals(groups[0]));
        AssertGroupResolution(result.Resolution.GroupResults, entries, groups, pots, capacity: 4);
    }

    [Fact]
    public void Group_RC8_fixed_same_pot_in_one_group_is_invalid()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 4);
        pots[entries[0]] = 1;
        pots[entries[1]] = 1;
        var fixedGroups = new[]
        {
            new GroupDrawPlacement(entries[0], groups[0]),
            new GroupDrawPlacement(entries[1], groups[0])
        };

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, 4, fixedGroups, seed: 1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC9_same_seed_is_reproducible()
    {
        var entries = NewEntries(12);
        var groups = NewGroups(3);
        var pots = BalancedPots(entries, 4);
        var r1 = DrawResolutionGenerator.Generate(GroupRequest(entries, groups, pots, 4, [], seed: 77));
        var r2 = DrawResolutionGenerator.Generate(GroupRequest(entries, groups, pots, 4, [], seed: 77));

        r1.IsResolved.Should().BeTrue();
        NormalizeGroupPlacements(r1.Resolution!.GroupResults)
            .Should().Equal(NormalizeGroupPlacements(r2.Resolution!.GroupResults));
    }

    [Fact]
    public void Group_RC_4_2_2_resolves()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, numberOfPots: 2);
        var request = GroupRequest(entries, groups, pots, 2, [], seed: 3);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        result.IsNoSolution.Should().BeFalse();
        AssertGroupResolution(result.Resolution!.GroupResults, entries, groups, pots, capacity: 2);
    }

    [Fact]
    public void Group_RC_different_seeds_remain_resolved_and_may_differ()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 4);
        var r1 = DrawResolutionGenerator.Generate(GroupRequest(entries, groups, pots, 4, [], seed: 1));
        var r2 = DrawResolutionGenerator.Generate(GroupRequest(entries, groups, pots, 4, [], seed: 2));

        r1.IsResolved.Should().BeTrue();
        r2.IsResolved.Should().BeTrue();
        AssertGroupResolution(r1.Resolution!.GroupResults, entries, groups, pots, capacity: 4);
        AssertGroupResolution(r2.Resolution!.GroupResults, entries, groups, pots, capacity: 4);

        // Seeds may produce different optima; inequality is not asserted.
    }

    [Fact]
    public void Group_RC_group_fully_fixed_completes_remaining()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = new Dictionary<EntryId, int>
        {
            [entries[0]] = 1, [entries[1]] = 1,
            [entries[2]] = 2, [entries[3]] = 2,
            [entries[4]] = 3, [entries[5]] = 3,
            [entries[6]] = 4, [entries[7]] = 4
        };

        // Group 0 fully filled by Fixed (one of each pot) — capacity 4.
        var fixedGroups = new[]
        {
            new GroupDrawPlacement(entries[0], groups[0]),
            new GroupDrawPlacement(entries[2], groups[0]),
            new GroupDrawPlacement(entries[4], groups[0]),
            new GroupDrawPlacement(entries[6], groups[0])
        };

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, 4, fixedGroups, seed: 6));

        result.IsResolved.Should().BeTrue();
        result.Resolution!.GroupResults.Where(p => p.GroupId.Equals(groups[0])).Should().HaveCount(4);
        result.Resolution.GroupResults.Where(p => p.GroupId.Equals(groups[1]))
            .Select(p => p.EntryId)
            .Should().BeEquivalentTo([entries[1], entries[3], entries[5], entries[7]]);
        AssertGroupResolution(result.Resolution.GroupResults, entries, groups, pots, capacity: 4);
    }

    [Fact]
    public void Group_RC_validated_request_under_G7_without_max_association_never_returns_no_solution()
    {
        // Documents G7 resolvability without MaxSameAssociationPerGroup.
        // With MaxSameAssociation, NoSolution becomes a real business case (see dedicated RC).
        var scenarios = new[]
        {
            (Entries: 4, Groups: 2, Pots: 2, Seed: 1),
            (Entries: 9, Groups: 3, Pots: 3, Seed: 2),
            (Entries: 16, Groups: 4, Pots: 4, Seed: 3),
            (Entries: 8, Groups: 2, Pots: 4, Seed: 4)
        };

        foreach (var (entryCount, groupCount, potCount, seed) in scenarios)
        {
            var entries = NewEntries(entryCount);
            var groups = NewGroups(groupCount);
            var pots = BalancedPots(entries, potCount);
            var result = DrawResolutionGenerator.Generate(
                GroupRequest(entries, groups, pots, potCount, [], seed));

            result.IsNoSolution.Should().BeFalse(
                because: "G3–G7 validated Group requests without MaxSameAssociation are always resolvable");
            result.IsResolved.Should().BeTrue();
        }
    }

    [Fact]
    public void Group_RC_max_association_balanced_resolves()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 4);
        var associations = FourAssociationsTwice(entries);

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                4,
                [],
                seed: 5,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsResolved.Should().BeTrue();
        AssertGroupResolution(result.Resolution!.GroupResults, entries, groups, pots, capacity: 4);
        AssertMaxAssociation(result.Resolution.GroupResults, associations, maxPerGroup: 1);
    }

    [Fact]
    public void Group_RC_max_association_oversized_returns_no_solution()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);
        var association = AssociationId.New();
        var associations = entries.ToDictionary(e => e, _ => association);

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                [],
                seed: 1,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsNoSolution.Should().BeTrue();
        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Group_RC_max_association_two_saves_otherwise_impossible()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);
        var association = AssociationId.New();
        var associations = entries.ToDictionary(e => e, _ => association);

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                [],
                seed: 1,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(2)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsResolved.Should().BeTrue();
        AssertMaxAssociation(result.Resolution!.GroupResults, associations, maxPerGroup: 2);
    }

    [Fact]
    public void Group_RC_max_association_incomplete_map_is_invalid()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);
        var associations = new Dictionary<EntryId, AssociationId>
        {
            [entries[0]] = AssociationId.New(),
            [entries[1]] = AssociationId.New(),
            [entries[2]] = AssociationId.New()
        };

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                [],
                seed: 1,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)],
                context: new DrawConstraintContext(null, null, associations)));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC_pairing_constraint_on_group_is_invalid()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                [],
                seed: 1,
                constraints: [new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)],
                context: DrawConstraintContext.Empty));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC_fixed_violating_max_association_returns_no_solution()
    {
        // Fixed are structurally valid (pots/capacity) but already exceed Max → NoSolution (not Invalid).
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);
        var association = AssociationId.New();
        var associations = entries.ToDictionary(e => e, _ => association);
        var fixedGroups = new[]
        {
            new GroupDrawPlacement(entries[0], groups[0]),
            new GroupDrawPlacement(entries[2], groups[0])
        };

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                fixedGroups,
                seed: 1,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsNoSolution.Should().BeTrue();
        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Group_RC_fixed_pots_and_max_together_return_no_solution()
    {
        // Each Fixed alone respects Max=1; pots force another same-association entry into the fixed group.
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = new Dictionary<EntryId, int>
        {
            [entries[0]] = 1, [entries[1]] = 1,
            [entries[2]] = 2, [entries[3]] = 2
        };
        var associationA = AssociationId.New();
        var associationB = AssociationId.New();
        var associations = new Dictionary<EntryId, AssociationId>
        {
            [entries[0]] = associationA,
            [entries[1]] = associationB,
            [entries[2]] = associationA,
            [entries[3]] = associationB
        };

        // G0 has pot1=A; G1 has pot2=B → remaining pot2 A must join G0 → 2×A in G0.
        var fixedGroups = new[]
        {
            new GroupDrawPlacement(entries[0], groups[0]),
            new GroupDrawPlacement(entries[3], groups[1])
        };

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                fixedGroups,
                seed: 1,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsNoSolution.Should().BeTrue();
    }

    [Fact]
    public void Group_RC_duplicate_max_association_constraint_is_invalid()
    {
        var entries = NewEntries(4);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 2);
        var associations = entries.ToDictionary(e => e, _ => AssociationId.New());

        var act = () => DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                2,
                [],
                seed: 1,
                constraints:
                [
                    DrawConstraint.MaxSameAssociationPerGroup(1),
                    DrawConstraint.MaxSameAssociationPerGroup(2)
                ],
                context: new DrawConstraintContext(null, null, associations)));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Group_RC_max_association_with_fixed_and_pots_resolves()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 4);
        var a1 = AssociationId.New();
        var a2 = AssociationId.New();
        var associations = new Dictionary<EntryId, AssociationId>
        {
            [entries[0]] = a1, [entries[1]] = a2,
            [entries[2]] = a2, [entries[3]] = a1,
            [entries[4]] = a1, [entries[5]] = a2,
            [entries[6]] = a2, [entries[7]] = a1
        };
        var fixedGroups = new[] { new GroupDrawPlacement(entries[0], groups[0]) };

        var result = DrawResolutionGenerator.Generate(
            GroupRequest(
                entries,
                groups,
                pots,
                4,
                fixedGroups,
                seed: 9,
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(2)],
                context: new DrawConstraintContext(null, null, associations)));

        result.IsResolved.Should().BeTrue();
        result.Resolution!.GroupResults.Should().Contain(p => p.EntryId.Equals(entries[0]) && p.GroupId.Equals(groups[0]));
        AssertMaxAssociation(result.Resolution.GroupResults, associations, maxPerGroup: 2);
    }

    [Fact]
    public void Group_RC_max_association_same_seed_is_reproducible()
    {
        var entries = NewEntries(8);
        var groups = NewGroups(2);
        var pots = BalancedPots(entries, 4);
        var associations = FourAssociationsTwice(entries);
        var constraints = new[] { DrawConstraint.MaxSameAssociationPerGroup(1) };
        var context = new DrawConstraintContext(null, null, associations);

        var r1 = DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, 4, [], 42, constraints, context));
        var r2 = DrawResolutionGenerator.Generate(
            GroupRequest(entries, groups, pots, 4, [], 42, constraints, context));

        r1.IsResolved.Should().BeTrue();
        NormalizeGroupPlacements(r1.Resolution!.GroupResults)
            .Should().Equal(NormalizeGroupPlacements(r2.Resolution!.GroupResults));
    }

    [Fact]
    public void Invalid_group_without_targets_throws()
    {
        var entries = NewEntries(4);
        var pots = BalancedPots(entries, 2);
        var request = new DrawGenerationRequest(
            DrawResolutionKind.Group,
            entries,
            [],
            DrawConstraintContext.Empty,
            new SeededSource(1),
            numberOfPots: 2,
            potMembership: new PotMembership(pots));

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Invalid_slot_coverage_throws()
    {
        var entries = NewEntries(3);
        var request = SlotRequest(entries, ["A", "B"], [], [], seed: 1);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Invalid_fixed_outside_targets_throws()
    {
        var entries = NewEntries(2);
        var request = SlotRequest(
            entries,
            ["A", "B"],
            [new SlotDrawPlacement(entries[0], "Z")],
            [],
            seed: 1);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    private static DrawGenerationRequest SlotRequest(
        IReadOnlyList<EntryId> entries,
        IReadOnlyList<string> targets,
        IReadOnlyList<SlotDrawPlacement> fixedSlots,
        IReadOnlyList<DrawConstraint> constraints,
        int seed) =>
        new(
            DrawResolutionKind.Slot,
            entries,
            constraints,
            DrawConstraintContext.Empty,
            new SeededSource(seed),
            targets,
            fixedSlots);

    private static DrawGenerationRequest GroupRequest(
        IReadOnlyList<EntryId> entries,
        IReadOnlyList<GroupId> groupTargets,
        IReadOnlyDictionary<EntryId, int> pots,
        int numberOfPots,
        IReadOnlyList<GroupDrawPlacement> fixedGroups,
        int seed,
        IReadOnlyList<DrawConstraint>? constraints = null,
        DrawConstraintContext? context = null) =>
        new(
            DrawResolutionKind.Group,
            entries,
            constraints ?? [],
            context ?? DrawConstraintContext.Empty,
            new SeededSource(seed),
            groupTargets: groupTargets,
            numberOfPots: numberOfPots,
            potMembership: new PotMembership(pots),
            fixedGroups: fixedGroups);

    private static GroupId[] NewGroups(int count) =>
        [..Enumerable.Range(0, count).Select(_ => GroupId.New())];

    private static Dictionary<EntryId, int> BalancedPots(EntryId[] entries, int numberOfPots)
    {
        var groupsCount = entries.Length / numberOfPots;
        var pots = new Dictionary<EntryId, int>();
        var index = 0;
        for (var pot = 1; pot <= numberOfPots; pot++)
        {
            for (var i = 0; i < groupsCount; i++)
            {
                pots[entries[index++]] = pot;
            }
        }

        return pots;
    }

    private static Dictionary<EntryId, AssociationId> FourAssociationsTwice(EntryId[] entries)
    {
        var associations = new[]
        {
            AssociationId.New(), AssociationId.New(), AssociationId.New(), AssociationId.New()
        };

        // Per pot of size 2: distinct associations; each association appears twice overall.
        return new Dictionary<EntryId, AssociationId>
        {
            [entries[0]] = associations[0], [entries[1]] = associations[1],
            [entries[2]] = associations[2], [entries[3]] = associations[3],
            [entries[4]] = associations[0], [entries[5]] = associations[1],
            [entries[6]] = associations[2], [entries[7]] = associations[3]
        };
    }

    private static void AssertMaxAssociation(
        IReadOnlyList<GroupDrawPlacement> placements,
        Dictionary<EntryId, AssociationId> associations,
        int maxPerGroup)
    {
        foreach (var group in placements.GroupBy(p => p.GroupId))
        {
            foreach (var byAssociation in group.GroupBy(p => associations[p.EntryId]))
            {
                byAssociation.Count().Should().BeLessThanOrEqualTo(maxPerGroup);
            }
        }
    }

    private static void AssertGroupResolution(
        IReadOnlyList<GroupDrawPlacement> placements,
        EntryId[] entries,
        IReadOnlyList<GroupId> groups,
        Dictionary<EntryId, int> pots,
        int capacity)
    {
        placements.Should().HaveCount(entries.Length);
        placements.Select(p => p.EntryId).Should().BeEquivalentTo(entries);
        foreach (var groupId in groups)
        {
            var inGroup = placements.Where(p => p.GroupId.Equals(groupId)).ToList();
            inGroup.Should().HaveCount(capacity);
            inGroup.Select(p => pots[p.EntryId]).Should().OnlyHaveUniqueItems();
            inGroup.Select(p => pots[p.EntryId]).Should().BeEquivalentTo(Enumerable.Range(1, capacity));
        }
    }

    private static List<(Guid Entry, Guid Group)> NormalizeGroupPlacements(IReadOnlyList<GroupDrawPlacement> placements) =>
        [..placements
            .Select(p => (Entry: p.EntryId.Value, Group: p.GroupId.Value))
            .OrderBy(t => t.Entry)
            .ThenBy(t => t.Group)];

    private static EntryId[] NewEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    /// <summary>
    /// Test double — Domain may use any <see cref="IRandomSource"/>; not Application SeededRandomSource.
    /// </summary>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Non-cryptographic test double for draw reproducibility.")]
    private sealed class SeededSource(int seed) : IRandomSource
    {
        private readonly Random _random = new(seed);

        public int NextInt32(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public double NextDouble() => _random.NextDouble();

        public void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
    }
}
