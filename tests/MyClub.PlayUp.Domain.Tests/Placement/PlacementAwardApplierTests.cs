// -----------------------------------------------------------------------
// <copyright file="PlacementAwardApplierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Placement;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Placement;

public sealed class PlacementAwardApplierTests
{
    private const string SourcePairKey = "P1";
    private readonly EntryId _winner = EntryId.New();
    private readonly EntryId _loser = EntryId.New();

    [Fact]
    public void Apply_winner_maps_winner_entry_and_rank()
    {
        var path = new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Winner, rank: 3);
        var outcome = new FixtureOutcome(_winner, _loser);

        var result = PlacementAwardApplier.Apply(path, SourcePairKey, outcome);

        result.Rank.Should().Be(3);
        result.EntryId.Should().Be(_winner);
    }

    [Fact]
    public void Apply_loser_maps_loser_entry_and_rank()
    {
        var path = new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Loser, rank: 4);
        var outcome = new FixtureOutcome(_winner, _loser);

        var result = PlacementAwardApplier.Apply(path, SourcePairKey, outcome);

        result.Rank.Should().Be(4);
        result.EntryId.Should().Be(_loser);
    }

    [Fact]
    public void Apply_rejects_source_pair_key_mismatch()
    {
        var path = new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Winner, rank: 1);
        var outcome = new FixtureOutcome(_winner, _loser);

        var act = () => PlacementAwardApplier.Apply(path, "P2", outcome);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.PlacementAwardApplyFixtureMismatch);
    }

    [Fact]
    public void ApplyForSource_returns_both_ranks_ordered()
    {
        const string otherKey = "P2";
        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Loser, rank: 4),
            new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Winner, rank: 3),
            new PlacementAwardPath(otherKey, ProgressionOutcome.Winner, rank: 1)
        ]);
        var outcome = new FixtureOutcome(_winner, _loser);

        var results = PlacementAwardApplier.ApplyForSource(rules, SourcePairKey, outcome);

        results.Should().HaveCount(2);
        results[0].Should().Be(new FinalPlacementInstruction(3, _winner));
        results[1].Should().Be(new FinalPlacementInstruction(4, _loser));
    }

    [Fact]
    public void ApplyForSource_returns_empty_when_no_path_matches()
    {
        var rules = new PlacementAwardRules(
        [
            new PlacementAwardPath("P2", ProgressionOutcome.Winner, rank: 1)
        ]);
        var outcome = new FixtureOutcome(_winner, _loser);

        PlacementAwardApplier.ApplyForSource(rules, SourcePairKey, outcome).Should().BeEmpty();
    }

    [Fact]
    public void Apply_is_deterministic_for_same_inputs()
    {
        var path = new PlacementAwardPath(SourcePairKey, ProgressionOutcome.Winner, rank: 1);
        var outcome = new FixtureOutcome(_winner, _loser);

        var first = PlacementAwardApplier.Apply(path, SourcePairKey, outcome);
        var second = PlacementAwardApplier.Apply(path, SourcePairKey, outcome);

        second.Should().Be(first);
    }
}
