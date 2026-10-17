// -----------------------------------------------------------------------
// <copyright file="ProgressionImpactWitnessCompositionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages.ProgressionImpact;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ProgressionImpactWitnessCompositionTests
{
    [Fact]
    public void Combine_prioritizes_wc1_consumed_loss_over_wc2() =>
        ProgressionImpactWitnessComposition.Combine(
                ProgressionPathConsumptionWitness.Verdict.ConsumedLoss,
                ProgressionPathProductionParticipationWitness.Verdict.NotApplicable)
            .Should()
            .Be(ProgressionImpactWitnessComposition.CombinedBlockReason.FeedConsumed);

    [Fact]
    public void Combine_maps_wc2_participating_when_wc1_not_consumed() =>
        ProgressionImpactWitnessComposition.Combine(
                ProgressionPathConsumptionWitness.Verdict.NotConsumed,
                ProgressionPathProductionParticipationWitness.Verdict.Participating)
            .Should()
            .Be(ProgressionImpactWitnessComposition.CombinedBlockReason.DownstreamProduction);

    [Fact]
    public void Combine_distinguishes_ambiguous_production_from_not_consumed() =>
        ProgressionImpactWitnessComposition.Combine(
                ProgressionPathConsumptionWitness.Verdict.NotConsumed,
                ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous)
            .Should()
            .Be(ProgressionImpactWitnessComposition.CombinedBlockReason.DownstreamProductionAmbiguous);
}
