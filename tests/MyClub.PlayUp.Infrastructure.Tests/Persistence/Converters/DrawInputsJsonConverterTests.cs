// -----------------------------------------------------------------------
// <copyright file="DrawInputsJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class DrawInputsJsonConverterTests
{
    private readonly DrawInputsJsonConverter _converter = new();

    [Fact]
    public void Convert_round_trips_null() => _converter.ConvertFromProvider(_converter.ConvertToProvider(null)).Should().BeNull();

    [Fact]
    public void Convert_round_trips_slot_inputs_with_seeds_pots_and_fixed()
    {
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var inputs = DrawInputs.ForSlot(
            [entryA, entryB],
            new SeedMap(new Dictionary<EntryId, int> { [entryA] = 1 }),
            new PotMembership(new Dictionary<EntryId, int> { [entryA] = 1, [entryB] = 2 }),
            [new SlotDrawPlacement(entryA, "W1")]);

        var json = _converter.ConvertToProvider(inputs).Should().BeOfType<string>().Subject;
        var restored = (DrawInputs)_converter.ConvertFromProvider(json)!;

        Structural(restored, inputs);
        json.Should().Contain("\"Entries\"");
        json.Should().Contain("\"Seeds\"");
        json.Should().Contain("\"FixedSlots\"");
        json.Should().NotContain("FixedGroups");
    }

    [Fact]
    public void Convert_round_trips_group_and_pairing_fixed_lists()
    {
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var groupId = GroupId.New();

        var groupInputs = DrawInputs.ForGroup([entryA, entryB], fixedPlacements: [new GroupDrawPlacement(entryA, groupId)]);
        Structural(
            (DrawInputs)_converter.ConvertFromProvider(_converter.ConvertToProvider(groupInputs))!,
            groupInputs);

        var pairingInputs = DrawInputs.ForPairing([entryA, entryB], fixedPlacements: [new PairingDrawResult(entryA, entryB)]);
        Structural(
            (DrawInputs)_converter.ConvertFromProvider(_converter.ConvertToProvider(pairingInputs))!,
            pairingInputs);
    }

    [Fact]
    public void Comparer_detects_structural_difference()
    {
        var entry = EntryId.New();
        var left = DrawInputs.ForSlot([entry]);
        var right = DrawInputs.ForSlot([entry], fixedPlacements: [new SlotDrawPlacement(entry, "A")]);

        DrawInputsJsonConverter.Comparer.Equals(left, right).Should().BeFalse();
        DrawInputsJsonConverter.Comparer.Equals(left, left.Copy()).Should().BeTrue();
    }

    private static void Structural(DrawInputs actual, DrawInputs expected)
    {
        actual.Entries.Should().Equal(expected.Entries);
        actual.FixedSlots.Should().Equal(expected.FixedSlots);
        actual.FixedGroups.Should().Equal(expected.FixedGroups);
        actual.FixedPairings.Should().Equal(expected.FixedPairings);
        if (expected.SeedMap is null)
        {
            actual.SeedMap.Should().BeNull();
        }
        else
        {
            actual.SeedMap!.Seeds.Should().BeEquivalentTo(expected.SeedMap.Seeds);
        }

        if (expected.PotMembership is null)
        {
            actual.PotMembership.Should().BeNull();
        }
        else
        {
            actual.PotMembership!.Pots.Should().BeEquivalentTo(expected.PotMembership.Pots);
        }
    }
}
