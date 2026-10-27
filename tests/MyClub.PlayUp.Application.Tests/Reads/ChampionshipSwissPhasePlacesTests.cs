// -----------------------------------------------------------------------
// <copyright file="ChampionshipSwissPhasePlacesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Championship / Swiss Places N decision (root occupying, fed expected occupancy).
/// </summary>
public sealed class ChampionshipSwissPhasePlacesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_root_non_positive_expected_occupancy_is_null(int expectedOccupancy)
    {
        ChampionshipSwissPhasePlaces
            .ResolvePlacesN(occupyingEntryCount: 8, isRootComposition: false, expectedOccupancy)
            .Should()
            .BeNull();
    }

    [Fact]
    public void Non_root_positive_expected_occupancy_is_that_count()
    {
        ChampionshipSwissPhasePlaces
            .ResolvePlacesN(occupyingEntryCount: 8, isRootComposition: false, expectedOccupancy: 4)
            .Should()
            .Be(4);
    }

    [Fact]
    public void Root_places_stay_occupying_when_expected_occupancy_is_not_positive()
    {
        ChampionshipSwissPhasePlaces
            .ResolvePlacesN(occupyingEntryCount: 8, isRootComposition: true, expectedOccupancy: 0)
            .Should()
            .Be(8);
    }

    [Fact]
    public void No_occupying_entries_is_null_even_when_fed_expected_occupancy_is_positive()
    {
        ChampionshipSwissPhasePlaces
            .ResolvePlacesN(occupyingEntryCount: 0, isRootComposition: false, expectedOccupancy: 4)
            .Should()
            .BeNull();
    }
}
