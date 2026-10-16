// -----------------------------------------------------------------------
// <copyright file="CupBracketTopologyClassifierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class CupBracketTopologyClassifierTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Classify_returns_NotApplicable_for_multi_round()
    {
        var stage = CreateMonoRoundShell();
        stage.AddRound("Second", _clock);
        AddCanonicalSlots(stage, 4);

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.NotApplicable);
    }

    [Fact]
    public void Classify_returns_ExplicitTopology_for_custom_slot_keys()
    {
        var stage = CreateMonoRoundShell();
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.SeedEntryRoundBracketPairs();

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.ExplicitTopology);
    }

    [Fact]
    public void Classify_returns_InvalidCanonicalShape_for_fifteen_slots()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 15);

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.InvalidCanonicalShape);
    }

    [Fact]
    public void Classify_returns_CanonicalIncomplete_when_pairs_missing()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 4);

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.CanonicalIncomplete);
    }

    [Fact]
    public void Classify_returns_CanonicalComplete_when_all_pairs_present()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 4);
        stage.SeedEntryRoundBracketPairs();

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.CanonicalComplete);
    }

    [Fact]
    public void Classify_returns_ExplicitTopology_when_existing_pair_misaligned()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 4);
        stage.ReplaceBracketPairs([new BracketPair("P1", "S1", "S3")]);

        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.ExplicitTopology);
    }

    [Fact]
    public void EnsureMissing_adds_all_pairs_when_empty()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 16);

        var added = CupCanonicalBracketPairEnsurer.EnsureMissing(stage);

        added.Should().Be(8);
        stage.BracketPairs.Should().HaveCount(8);
        CupBracketTopologyClassifier.Classify(stage).Should().Be(CupBracketTopologyKind.CanonicalComplete);
    }

    [Fact]
    public void EnsureMissing_is_idempotent()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 8);
        stage.SeedEntryRoundBracketPairs();
        stage.ClearBracketPairs();
        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4"),
        ]);

        CupCanonicalBracketPairEnsurer.EnsureMissing(stage).Should().Be(2);
        CupCanonicalBracketPairEnsurer.EnsureMissing(stage).Should().Be(0);
        stage.BracketPairs.Should().HaveCount(4);
    }

    [Fact]
    public void EnsureMissing_does_not_mutate_explicit_topology()
    {
        var stage = CreateMonoRoundShell();
        stage.AddSlot("QF-1-A");
        stage.AddSlot("QF-1-B");
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "F-A", "F-B")]);

        CupCanonicalBracketPairEnsurer.EnsureMissing(stage).Should().Be(0);
        stage.BracketPairs.Should().HaveCount(1);
    }

    [Fact]
    public void EnsureMissing_adds_only_missing_pairs_when_some_already_exist()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 8);
        stage.SeedEntryRoundBracketPairs();
        _ = stage.AddFixture(stage.Rounds[0].Id, _clock, "S1", "S2", "P1");
        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4"),
        ]);

        CupCanonicalBracketPairEnsurer.EnsureMissing(stage).Should().Be(2);
        stage.FindBracketPair("P1").Should().NotBeNull();
        stage.FindBracketPair("P3")!.SlotAKey.Should().Be("S5");
        stage.FindFixtureByBracketPairKey("P1").Should().NotBeNull();
    }

    [Fact]
    public void AppendCanonicalBracketPairs_works_in_Ready()
    {
        var stage = CreateMonoRoundShell();
        AddCanonicalSlots(stage, 4);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        stage.AppendCanonicalBracketPairs([new BracketPair("P1", "S1", "S2")]);

        stage.BracketPairs.Should().ContainSingle();
    }

    private Stage CreateMonoRoundShell()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Tour", _clock);
        return stage;
    }

    private static void AddCanonicalSlots(Stage stage, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            stage.AddSlot($"S{index}");
        }
    }
}
