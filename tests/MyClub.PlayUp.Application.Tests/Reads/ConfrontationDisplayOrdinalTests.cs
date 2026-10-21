// -----------------------------------------------------------------------
// <copyright file="ConfrontationDisplayOrdinalTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class ConfrontationDisplayOrdinalTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TryGetForPairKey_orders_by_PairKey()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.AddSlot("C");
        stage.AddSlot("D");
        stage.ReplaceBracketPairs([
            new BracketPair("P2", "C", "D"),
            new BracketPair("P1", "A", "B")
        ]);

        ConfrontationDisplayOrdinal.TryGetForPairKey(stage, "P1").Should().Be(1);
        ConfrontationDisplayOrdinal.TryGetForPairKey(stage, "P2").Should().Be(2);
    }

    [Fact]
    public void TryGetForFixture_uses_pair_ordinal_when_BracketPairKey_set()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "SF1-A", "SF1-B")]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");

        ConfrontationDisplayOrdinal.TryGetForFixture(stage, fixture.Id).Should().Be(1);
        ConfrontationDisplayOrdinal.TryGetForPairKey(stage, "P1").Should().Be(1);
    }
}
