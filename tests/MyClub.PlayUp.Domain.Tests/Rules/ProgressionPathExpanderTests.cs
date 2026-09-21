// -----------------------------------------------------------------------
// <copyright file="ProgressionPathExpanderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class ProgressionPathExpanderTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Materialize_expands_one_intent_to_all_round_fixtures()
    {
        var stage = CreateCupWithFixtures(2);
        var round = stage.Rounds[0];
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            order: 1,
            round.Id,
            ProgressionOutcome.Winner,
            peer);

        var paths = ProgressionPathExpander.Materialize([intent], stage.Rounds);

        paths.Should().HaveCount(2);
        paths.Should().OnlyContain(p =>
            p.Outcome == ProgressionOutcome.Winner && p.Destination.TargetsPopulation);
        paths.Select(p => p.SourceFixtureId).Should().BeEquivalentTo(round.Fixtures.Select(f => f.Id));
    }

    [Fact]
    public void Materialize_Place_zips_slot_keys_to_fixtures()
    {
        var stage = CreateCupWithFixtures(2);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Winner,
            stage.Id,
            ["SF1-A", "SF1-B"]);

        var paths = ProgressionPathExpander.Materialize([intent], stage.Rounds);

        paths.Should().HaveCount(2);
        paths[0].Destination.SlotKey.Should().Be("SF1-A");
        paths[1].Destination.SlotKey.Should().Be("SF1-B");
        paths.Select(p => p.SourceFixtureId).Should().Equal(stage.Rounds[0].Fixtures.Select(f => f.Id));
    }

    [Fact]
    public void Materialize_rejects_Place_when_slot_key_count_mismatches_fixtures()
    {
        var stage = CreateCupWithFixtures(2);
        stage.AddSlot("SF1-A");
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Winner,
            stage.Id,
            ["SF1-A"]);

        var act = () => ProgressionPathExpander.Materialize([intent], stage.Rounds);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_Place_slot_keys()
    {
        var act = () => new ProgressionIntent(
            IntentId.New(),
            1,
            RoundId.New(),
            ProgressionOutcome.Winner,
            StageId.New(),
            ["SF1-A", "SF1-A"]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void FromIntents_stores_intents_and_derived_paths()
    {
        var stage = CreateCupWithFixtures(2);
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Loser,
            peer);

        var rules = ProgressionRules.FromIntents([intent], stage.Rounds);

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceProgressionRules_allows_cross_stage_place()
    {
        var source = CreateCupWithFixtures(1);
        var peer = Stage.Create(
            source.CompetitionId,
            new StageName("SF"),
            SampleRegulations.Standard(),
            _clock);
        peer.AddSlot("SF1-A");
        var fixture = source.Rounds[0].Fixtures[0];

        var act = () => source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(peer.Id, "SF1-A"))
            ]),
            _clock);

        act.Should().NotThrow();
        source.Regulation.ProgressionRules!.Paths.Should().ContainSingle()
            .Which.Destination.Should().Be(ProgressionDestination.ForSlot(peer.Id, "SF1-A"));
    }

    private Stage CreateCupWithFixtures(int fixtureCount)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("QF"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        for (var i = 0; i < fixtureCount; i++)
        {
            stage.AddFixture(round.Id, _clock);
        }

        return stage;
    }
}
