// -----------------------------------------------------------------------
// <copyright file="TieFormatEffectiveResolutionTruthTableTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class TieFormatEffectiveResolutionTruthTableTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 21, 0, 0, TimeSpan.Zero));

    public static TheoryData<TieFormat?, TieFormat?, TieFormat> Matrix =>
        new()
        {
            { null, null, TieFormat.DefaultOneLeg },
            { null, new TieFormat(TieFormat.TwoLegs, true), new TieFormat(TieFormat.TwoLegs, true) },
            {
                new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule()),
                new TieFormat(TieFormat.TwoLegs, true),
                new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule())
            },
            {
                null,
                new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule()),
                new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule())
            },
        };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Effective_resolution_is_consistent_across_domain_and_application(
        TieFormat? roundStored,
        TieFormat? stageDefault,
        TieFormat expected)
    {
        var stage = CreateStage(stageDefault);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var round = stage.AddRound("R1", roundStored, _clock);
        stage.SeedEntryRoundBracketPairs();
        var pairKey = stage.BracketPairs[0].PairKey;

        var domain = Stage.ResolveEffectiveTieFormat(round, stage.Regulation);
        var keyed = stage.ResolveEffectiveTieFormatForKey(pairKey);
        var legs = CupConfrontationMaterializer.ExpectedLegsForRound(stage, round);
        domain.Should().BeEquivalentTo(expected);
        keyed.Should().BeEquivalentTo(expected);
        legs.Should().Be(expected.NumberOfLegs);
        TieFormat.ResolveEffective(round.TieFormat, stage.Regulation.TieFormat)
            .Should()
            .BeEquivalentTo(expected);
    }

    private Stage CreateStage(TieFormat? defaultTie)
    {
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            defaultTie);
        var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), regulation, _clock);
        return stage;
    }
}
