// -----------------------------------------------------------------------
// <copyright file="ProgressionApplierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Progression;

public sealed class ProgressionApplierTests
{
    private readonly FixtureId _fixtureId = FixtureId.New();
    private readonly StageId _destinationStageId = StageId.New();
    private readonly EntryId _winner = EntryId.New();
    private readonly EntryId _loser = EntryId.New();

    [Fact]
    public void Apply_winner_maps_winner_entry_and_destination()
    {
        var path = Path(ProgressionOutcome.Winner, "SF1-A");
        var outcome = new FixtureOutcome(_winner, _loser);

        var result = ProgressionApplier.Apply(path, _fixtureId, outcome);

        result.StageId.Should().Be(_destinationStageId);
        result.SlotKey.Should().Be("SF1-A");
        result.EntryId.Should().Be(_winner);
    }

    [Fact]
    public void Apply_loser_maps_loser_entry_and_destination()
    {
        var path = Path(ProgressionOutcome.Loser, "Consolante-1");
        var outcome = new FixtureOutcome(_winner, _loser);

        var result = ProgressionApplier.Apply(path, _fixtureId, outcome);

        result.StageId.Should().Be(_destinationStageId);
        result.SlotKey.Should().Be("Consolante-1");
        result.EntryId.Should().Be(_loser);
    }

    [Fact]
    public void Apply_rejects_fixture_id_mismatch()
    {
        var path = Path(ProgressionOutcome.Winner, "SF1-A");
        var outcome = new FixtureOutcome(_winner, _loser);

        var act = () => ProgressionApplier.Apply(path, FixtureId.New(), outcome);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.ProgressionApplyFixtureMismatch);
    }

    [Fact]
    public void Apply_is_deterministic_for_same_inputs()
    {
        var path = Path(ProgressionOutcome.Winner, "SF1-A");
        var outcome = new FixtureOutcome(_winner, _loser);

        var first = ProgressionApplier.Apply(path, _fixtureId, outcome);
        var second = ProgressionApplier.Apply(path, _fixtureId, outcome);

        second.Should().Be(first);
    }

    [Fact]
    public void Apply_rejects_null_path()
    {
        var outcome = new FixtureOutcome(_winner, _loser);

        var act = () => ProgressionApplier.Apply(null!, _fixtureId, outcome);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("path");
    }

    [Fact]
    public void Apply_rejects_null_outcome()
    {
        var path = Path(ProgressionOutcome.Winner, "SF1-A");

        var act = () => ProgressionApplier.Apply(path, _fixtureId, null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("outcome");
    }

    [Fact]
    public void Apply_population_destination_omits_slot_key()
    {
        var remoteStageId = StageId.New();
        var path = new ProgressionPath(
            _fixtureId,
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(remoteStageId));
        var outcome = new FixtureOutcome(_winner, _loser);

        var result = ProgressionApplier.Apply(path, _fixtureId, outcome);

        result.StageId.Should().Be(remoteStageId);
        result.SlotKey.Should().BeNull();
        result.TargetsPopulation.Should().BeTrue();
        result.EntryId.Should().Be(_winner);
    }

    private ProgressionPath Path(ProgressionOutcome outcome, string slotKey) =>
        new(
            _fixtureId,
            outcome,
            new ProgressionDestination(_destinationStageId, slotKey));
}
