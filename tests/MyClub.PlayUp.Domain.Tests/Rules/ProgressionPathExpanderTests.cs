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
            ProgressionDestination.ForPopulation(peer));

        var paths = ProgressionPathExpander.Materialize([intent], stage.Rounds);

        paths.Should().HaveCount(2);
        paths.Should().OnlyContain(p =>
            p.Outcome == ProgressionOutcome.Winner && p.Destination.TargetsPopulation);
        paths.Select(p => p.SourceFixtureId).Should().BeEquivalentTo(round.Fixtures.Select(f => f.Id));
    }

    [Fact]
    public void Materialize_rejects_place_intent_when_round_has_multiple_fixtures()
    {
        var stage = CreateCupWithFixtures(2);
        stage.AddSlot("SF1-A");
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Winner,
            ProgressionDestination.ForSlot(stage.Id, "SF1-A"));

        var act = () => ProgressionPathExpander.Materialize([intent], stage.Rounds);

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
            ProgressionDestination.ForPopulation(peer));

        var rules = ProgressionRules.FromIntents([intent], stage.Rounds);

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_cross_stage_place()
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

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
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
