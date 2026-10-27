// -----------------------------------------------------------------------
// <copyright file="ProgressionImpactWitnessComposition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages.ProgressionImpact;

/// <summary>
/// Combines consumption and downstream-production witnesses for the progression impact gate.
/// </summary>
internal static class ProgressionImpactWitnessComposition
{
    internal enum CombinedBlockReason
    {
        None,
        FeedConsumed,
        FeedConsumedAmbiguous,
        DownstreamProduction,
        DownstreamProductionAmbiguous
    }

    internal static CombinedBlockReason Combine(
        ProgressionPathConsumptionWitness.Verdict wc1,
        ProgressionPathProductionParticipationWitness.Verdict wc2) =>
        wc1 switch
        {
            ProgressionPathConsumptionWitness.Verdict.ConsumedLoss => CombinedBlockReason.FeedConsumed,
            ProgressionPathConsumptionWitness.Verdict.ConsumedAmbiguous => CombinedBlockReason.FeedConsumedAmbiguous,
            _ => wc2 switch
            {
                ProgressionPathProductionParticipationWitness.Verdict.Participating => CombinedBlockReason
                    .DownstreamProduction,
                ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous => CombinedBlockReason
                    .DownstreamProductionAmbiguous,
                _ => CombinedBlockReason.None
            }
        };
}
