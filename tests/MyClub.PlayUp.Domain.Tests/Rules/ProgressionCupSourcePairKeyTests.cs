// -----------------------------------------------------------------------
// <copyright file="ProgressionCupSourcePairKeyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

/// <summary>
/// Cup V1 SourcePairKey — Save on pairs before fixtures (RC-PRE1).
/// </summary>
public sealed class ProgressionCupSourcePairKeyTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 21, 0, 0, TimeSpan.Zero));

    [Fact]
    public void RC_PRE1_Save_intents_on_BracketPairs_without_fixtures_then_materialize_Apply()
    {
        var stage = CreateCupWithPairsNoFixtures();
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            order: 1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Winner,
            peer);

        var rules = ProgressionRules.FromIntents([intent], stage.Rounds, stage.BracketPairs);
        stage.ReplaceProgressionRules(rules, _clock);

        var snapshotPaths = stage.Regulation.ProgressionRules!.Paths.Select(p => p.Copy()).ToArray();
        var snapshotIntents = stage.Regulation.ProgressionRules.Intents.Select(i => i.Copy()).ToArray();
        snapshotPaths.Should().HaveCount(2);
        snapshotPaths.Select(p => p.SourcePairKey).Should().Equal("P1", "P2");

        var p1 = stage.FindBracketPair("P1")!;
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, p1.SlotAKey, p1.SlotBKey, p1.PairKey);
        fixture.BracketPairKey.Should().Be("P1");

        stage.Regulation.ProgressionRules!.Paths.Should().BeEquivalentTo(snapshotPaths);
        stage.Regulation.ProgressionRules.Intents.Should().BeEquivalentTo(snapshotIntents);

        var winner = EntryId.New();
        var loser = EntryId.New();
        var path = stage.Regulation.ProgressionRules.Paths.Single(p => p.SourcePairKey == "P1");
        var instruction = ProgressionApplier.Apply(path, "P1", new FixtureOutcome(winner, loser));

        instruction.EntryId.Should().Be(winner);
        instruction.StageId.Should().Be(peer);
        instruction.TargetsPopulation.Should().BeTrue();
    }

    private Stage CreateCupWithPairsNoFixtures()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Tour principal", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.AddSlot("S3");
        stage.AddSlot("S4");
        stage.SeedEntryRoundBracketPairs();
        stage.BracketPairs.Should().HaveCount(2);
        stage.Rounds[0].Fixtures.Should().BeEmpty();
        return stage;
    }
}
