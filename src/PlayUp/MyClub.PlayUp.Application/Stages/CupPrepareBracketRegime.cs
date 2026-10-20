// -----------------------------------------------------------------------
// <copyright file="CupPrepareBracketRegime.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Prepare-stage Cup mono-round bracket regimes (contract A1).
/// </summary>
internal enum CupPrepareBracketRegime
{
    NotApplicable,
    R0_Unconfigured,
    R1_ExplicitTopology,
    R2_Canonical,
    R3_InvalidCanonical
}

internal static class CupPrepareBracketRegimeResolver
{
    internal static CupPrepareBracketRegime Resolve(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (StructureSkeleton.InferFormat(stage) != StructureFormatKind.Cup
            || !IsMonoRoundKnockout(stage))
        {
            return CupPrepareBracketRegime.NotApplicable;
        }

        if (stage.Slots.Count == 0)
        {
            return CupPrepareBracketRegime.R0_Unconfigured;
        }

        return CupBracketTopologyClassifier.Classify(stage) switch
        {
            CupBracketTopologyKind.InvalidCanonicalShape => CupPrepareBracketRegime.R3_InvalidCanonical,
            CupBracketTopologyKind.CanonicalComplete or CupBracketTopologyKind.CanonicalIncomplete =>
                CupPrepareBracketRegime.R2_Canonical,
            CupBracketTopologyKind.ExplicitTopology => CupPrepareBracketRegime.R1_ExplicitTopology,
            _ => CupPrepareBracketRegime.R1_ExplicitTopology
        };
    }

    private static bool IsMonoRoundKnockout(Stage stage) =>
        stage is { IsSwiss: false, Rounds.Count: 1, Groups.Count: 0, Matchdays.Count: 0 };
}
