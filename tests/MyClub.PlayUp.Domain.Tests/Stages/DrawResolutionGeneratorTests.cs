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
    public void RC3_pairing_same_group_required_avoids_intra_group()
    {
        var entries = NewEntries(8);
        var g1 = GroupId.New();
        var g2 = GroupId.New();
        var g3 = GroupId.New();
        var g4 = GroupId.New();
        var groups = new Dictionary<EntryId, GroupId>
        {
            [entries[0]] = g1, [entries[1]] = g1,
            [entries[2]] = g2, [entries[3]] = g2,
            [entries[4]] = g3, [entries[5]] = g3,
            [entries[6]] = g4, [entries[7]] = g4
        };
        var request = PairingRequest(
            entries,
            fixedPairings: [],
            constraints: [new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)],
            context: new DrawConstraintContext(groups, null),
            seed: 11);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsResolved.Should().BeTrue();
        foreach (var pair in result.Resolution!.PairingResults)
        {
            groups[pair.EntryA].Should().NotBe(groups[pair.EntryB]);
        }
    }

    [Fact]
    public void RC4_preferred_same_group_is_ignored()
    {
        var entries = NewEntries(4);
        var g = GroupId.New();
        var groups = entries.ToDictionary(e => e, _ => g);
        var request = PairingRequest(
            entries,
            [],
            [new DrawConstraint(DrawConstraintType.SameGroupAvoidance)],
            new DrawConstraintContext(groups, null),
            seed: 3);

        var result = DrawResolutionGenerator.Generate(request);

        // Preferred ignored → solution exists even if all same group.
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void RC5_no_solution_when_required_same_group_impossible()
    {
        var entries = NewEntries(4);
        var g1 = GroupId.New();

        // Make impossible: all four in same group with Required SameGroup.
        var groups = entries.ToDictionary(e => e, _ => g1);
        var request = PairingRequest(
            entries,
            [],
            [new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)],
            new DrawConstraintContext(groups, null),
            seed: 1);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
        result.Resolution.Should().BeNull();
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
    public void Invalid_group_kind_throws()
    {
        var entries = NewEntries(2);
        var request = new DrawGenerationRequest(
            DrawResolutionKind.Group,
            entries,
            [],
            DrawConstraintContext.Empty,
            new SeededSource(1));

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

    [Fact]
    public void Invalid_same_group_required_without_map_throws()
    {
        var entries = NewEntries(4);
        var request = PairingRequest(
            entries,
            [],
            [new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)],
            DrawConstraintContext.Empty,
            seed: 1);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Invalid_odd_pairing_pool_throws()
    {
        var request = PairingRequest(NewEntries(3), [], [], DrawConstraintContext.Empty, seed: 1);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Invalid_same_association_required_throws()
    {
        var request = PairingRequest(
            NewEntries(2),
            [],
            [new DrawConstraint(DrawConstraintType.SameAssociationAvoidance, ConstraintEnforcement.Required)],
            DrawConstraintContext.Empty,
            seed: 1);

        var act = () => DrawResolutionGenerator.Generate(request);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
    }

    [Fact]
    public void Same_team_required_rejects_same_team_pair()
    {
        var entries = NewEntries(4);
        var teamX = TeamId.New();
        var teamY = TeamId.New();
        var teams = new Dictionary<EntryId, TeamId>
        {
            [entries[0]] = teamX,
            [entries[1]] = teamX,
            [entries[2]] = teamY,
            [entries[3]] = teamY
        };

        // Fixed forces same-team pair → NoSolution.
        var request = PairingRequest(
            entries,
            [new PairingDrawResult(entries[0], entries[1])],
            [new DrawConstraint(DrawConstraintType.SameTeamAvoidance, ConstraintEnforcement.Required)],
            new DrawConstraintContext(null, teams),
            seed: 2);

        var result = DrawResolutionGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
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

    private static DrawGenerationRequest PairingRequest(
        IReadOnlyList<EntryId> entries,
        IReadOnlyList<PairingDrawResult> fixedPairings,
        IReadOnlyList<DrawConstraint> constraints,
        DrawConstraintContext context,
        int seed) =>
        new(
            DrawResolutionKind.Pairing,
            entries,
            constraints,
            context,
            new SeededSource(seed),
            null,
            null,
            fixedPairings);

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
