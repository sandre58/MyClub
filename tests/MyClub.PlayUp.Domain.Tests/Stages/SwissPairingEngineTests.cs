// -----------------------------------------------------------------------
// <copyright file="SwissPairingEngineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class SwissPairingEngineTests
{
    // Fixed Guids keep EntryId order deterministic across runs.
    private static readonly EntryId E1 = Id(1);
    private static readonly EntryId E2 = Id(2);
    private static readonly EntryId E3 = Id(3);
    private static readonly EntryId E4 = Id(4);
    private static readonly EntryId E5 = Id(5);
    private static readonly EntryId E6 = Id(6);
    private static readonly EntryId E7 = Id(7);
    private static readonly EntryId E8 = Id(8);

    [Fact]
    public void Case1_eight_teams_three_rounds_nominal_first_round()
    {
        var standings = EqualPoints([E1, E2, E3, E4, E5, E6, E7, E8]);

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, []));

        result.IsSuccess.Should().BeTrue();
        result.ByeEntryId.Should().BeNull();
        result.Pairings.Should().HaveCount(4);
        AssertCompleteCover(standings, result);
        AssertNoRematches(result, []);
    }

    [Fact]
    public void Case2_six_teams_three_rounds_first_round()
    {
        var standings = EqualPoints([E1, E2, E3, E4, E5, E6]);

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, []));

        result.IsSuccess.Should().BeTrue();
        result.Pairings.Should().HaveCount(3);
        AssertCompleteCover(standings, result);
    }

    [Fact]
    public void Case3_seven_teams_assigns_exactly_one_bye()
    {
        var standings = EqualPoints([E1, E2, E3, E4, E5, E6, E7]);

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, []));

        result.IsSuccess.Should().BeTrue();
        result.ByeEntryId.Should().NotBeNull();
        result.Pairings.Should().HaveCount(3);
        AssertCompleteCover(standings, result);
    }

    [Fact]
    public void Case3_bye_prefers_fewest_byes_then_worst_position()
    {
        var standings = EqualPoints([E1, E2, E3]);
        var byeCounts = new Dictionary<EntryId, int> { [E1] = 1, [E2] = 0, [E3] = 0 };

        var result = SwissPairingEngine.BuildPairings(
            new SwissPairingRequest(standings, [], byeCounts));

        // E3 is worst position among zero-bye (pos 3); E2 also 0 byes but better position.
        result.IsSuccess.Should().BeTrue();
        result.ByeEntryId.Should().Be(E3);
        result.Pairings.Should().ContainSingle()
            .Which.Should().Be(new SwissPairing(E1, E2));
    }

    [Fact]
    public void Case4_two_teams_minimal()
    {
        var standings = EqualPoints([E1, E2]);

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, []));

        result.IsSuccess.Should().BeTrue();
        result.Pairings.Should().ContainSingle().Which.Should().Be(new SwissPairing(E1, E2));
    }

    [Fact]
    public void Case5_avoids_rematch_when_alternative_exists()
    {
        var standings = EqualPoints([E1, E2, E3, E4]);
        var played = new List<(EntryId, EntryId)> { (E1, E2) };

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, played));

        result.IsSuccess.Should().BeTrue();
        AssertNoRematches(result, played);
        result.Pairings.Should().NotContain(p => Involves(p, E1, E2));
    }

    [Fact]
    public void Case5_no_solution_when_only_rematch_remains()
    {
        var standings = EqualPoints([E1, E2]);
        var played = new List<(EntryId, EntryId)> { (E1, E2) };

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, played));

        result.IsNoSolution.Should().BeTrue();
        result.Pairings.Should().BeEmpty();
    }

    [Fact]
    public void Case6_same_points_uses_position_tie_break_for_ordering()
    {
        // Same points; position already encodes StandingRules tie-break.
        var standings = new[]
        {
            new SwissParticipantStanding(E2, points: 3, position: 1),
            new SwissParticipantStanding(E1, points: 3, position: 2),
            new SwissParticipantStanding(E4, points: 0, position: 3),
            new SwissParticipantStanding(E3, points: 0, position: 4)
        };

        var result = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standings, []));

        result.IsSuccess.Should().BeTrue();

        // Top of table pairs with closest score group first (other 3pts), then EntryId/position.
        result.Pairings.Should().Contain(p => Involves(p, E2, E1));
        result.Pairings.Should().Contain(p => Involves(p, E4, E3));
    }

    [Fact]
    public void Case7_progressive_second_round_uses_updated_history()
    {
        var standingsR1 = EqualPoints([E1, E2, E3, E4]);
        var round1 = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standingsR1, []));
        round1.IsSuccess.Should().BeTrue();

        var played = round1.Pairings
            .Select(p => (p.HomeEntryId, p.AwayEntryId))
            .ToList();

        var standingsR2 = new[]
        {
            new SwissParticipantStanding(E1, 3, 1),
            new SwissParticipantStanding(E3, 3, 2),
            new SwissParticipantStanding(E2, 0, 3),
            new SwissParticipantStanding(E4, 0, 4)
        };

        var round2 = SwissPairingEngine.BuildPairings(new SwissPairingRequest(standingsR2, played));

        round2.IsSuccess.Should().BeTrue();
        AssertNoRematches(round2, played);
        AssertCompleteCover(standingsR2, round2);
    }

    [Fact]
    public void Case8_same_inputs_yield_identical_pairings()
    {
        var standings = EqualPoints([E1, E2, E3, E4, E5, E6, E7, E8]);
        var played = new List<(EntryId, EntryId)> { (E1, E2), (E3, E4) };
        var request = new SwissPairingRequest(standings, played);

        var first = SwissPairingEngine.BuildPairings(request);
        var second = SwissPairingEngine.BuildPairings(request);

        first.IsSuccess.Should().BeTrue();
        second.Pairings.Should().Equal(first.Pairings);
        second.ByeEntryId.Should().Be(first.ByeEntryId);
    }

    [Fact]
    public void Home_away_prefers_entry_with_fewer_prior_homes()
    {
        var standings = EqualPoints([E1, E2]);
        var homes = new Dictionary<EntryId, int> { [E1] = 2, [E2] = 0 };

        var result = SwissPairingEngine.BuildPairings(
            new SwissPairingRequest(standings, [], homeCountsByEntry: homes));

        result.Pairings.Should().ContainSingle().Which.Should().Be(new SwissPairing(E2, E1));
    }

    [Fact]
    public void Request_rejects_empty_standings()
    {
        var act = () => new SwissPairingRequest([], []);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissPairingInvalid);
    }

    private static EntryId Id(byte n) =>
        new(Guid.Parse($"00000000-0000-7000-8000-0000000000{n:D2}"));

    private static SwissParticipantStanding[] EqualPoints(IReadOnlyList<EntryId> entries) =>
        [.. entries.Select((id, index) => new SwissParticipantStanding(id, points: 0, position: index + 1))];

    private static void AssertCompleteCover(
        IReadOnlyList<SwissParticipantStanding> standings,
        SwissPairingResult result)
    {
        var covered = result.Pairings
            .SelectMany(p => new[] { p.HomeEntryId, p.AwayEntryId })
            .Concat(result.ByeEntryId is { } bye ? [bye] : [])
            .ToHashSet();

        covered.Should().BeEquivalentTo(standings.Select(s => s.EntryId));
    }

    private static void AssertNoRematches(
        SwissPairingResult result,
        IReadOnlyList<(EntryId First, EntryId Second)> played)
    {
        foreach (var pairing in result.Pairings)
        {
            played.Should().NotContain(p =>
                (p.First.Equals(pairing.HomeEntryId) && p.Second.Equals(pairing.AwayEntryId))
                || (p.First.Equals(pairing.AwayEntryId) && p.Second.Equals(pairing.HomeEntryId)));
        }
    }

    private static bool Involves(SwissPairing pairing, EntryId a, EntryId b) =>
        (pairing.HomeEntryId.Equals(a) && pairing.AwayEntryId.Equals(b))
        || (pairing.HomeEntryId.Equals(b) && pairing.AwayEntryId.Equals(a));
}
