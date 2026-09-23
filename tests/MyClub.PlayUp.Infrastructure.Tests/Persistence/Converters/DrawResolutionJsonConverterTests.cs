// -----------------------------------------------------------------------
// <copyright file="DrawResolutionJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class DrawResolutionJsonConverterTests
{
    private readonly DrawResolutionJsonConverter _converter = new();

    [Fact]
    public void Convert_round_trips_not_resolved_and_no_solution()
    {
        RoundTrip(DrawResolution.NotResolved());
        RoundTrip(DrawResolution.NoSolution());
    }

    [Fact]
    public void Convert_round_trips_resolved_slot_and_group()
    {
        var entryA = EntryId.New();

        RoundTrip(DrawResolution.ResolvedSlots([new SlotDrawPlacement(entryA, "SF1")]));
        RoundTrip(DrawResolution.ResolvedGroups([new GroupDrawPlacement(entryA, GroupId.New())]));
    }

    [Fact]
    public void Convert_uses_pascal_case_and_numeric_enums()
    {
        var json = _converter.ConvertToProvider(DrawResolution.NoSolution()).Should().BeOfType<string>().Subject;
        json.Should().Contain("\"State\":");
        json.Should().NotContain("\"state\":");
        json.Should().NotContain("NoSolution");
    }

    [Fact]
    public void Comparer_is_structural()
    {
        var left = DrawResolution.ResolvedSlots([new SlotDrawPlacement(EntryId.New(), "A")]);
        var right = left.Copy();
        DrawResolutionJsonConverter.Comparer.Equals(left, right).Should().BeTrue();
        DrawResolutionJsonConverter.Comparer.Equals(left, DrawResolution.NoSolution()).Should().BeFalse();
    }

    private void RoundTrip(DrawResolution resolution)
    {
        var restored = (DrawResolution)_converter.ConvertFromProvider(_converter.ConvertToProvider(resolution))!;
        restored.State.Should().Be(resolution.State);
        restored.ResolvedKind.Should().Be(resolution.ResolvedKind);
        restored.SlotResults.Should().Equal(resolution.SlotResults);
        restored.GroupResults.Should().Equal(resolution.GroupResults);
    }
}
