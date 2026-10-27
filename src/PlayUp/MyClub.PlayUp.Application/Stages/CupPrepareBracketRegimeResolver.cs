// -----------------------------------------------------------------------
// <copyright file="CupPrepareBracketRegimeResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Prepare-stage Cup mono-round bracket regimes.
/// </summary>
internal enum CupPrepareBracketRegime
{
    NotApplicable,
    R0Unconfigured,
    R1ExplicitTopology,
    R2Canonical,
    R3InvalidCanonical
}

internal static class CupPrepareBracketRegimeResolver
{
    internal static CupPrepareBracketRegime Resolve(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return StructureSkeleton.InferFormat(stage) != StructureFormatKind.Cup || !IsMonoRoundKnockout(stage) ? CupPrepareBracketRegime.NotApplicable : stage.Slots.Count == 0 ? CupPrepareBracketRegime.R0Unconfigured : CupBracketTopologyClassifier.Classify(stage) switch { CupBracketTopologyKind.InvalidCanonicalShape => CupPrepareBracketRegime.R3InvalidCanonical, CupBracketTopologyKind.CanonicalComplete or CupBracketTopologyKind.CanonicalIncomplete => CupPrepareBracketRegime.R2Canonical, CupBracketTopologyKind.ExplicitTopology => CupPrepareBracketRegime.R1ExplicitTopology, _ => CupPrepareBracketRegime.R1ExplicitTopology };
    }

    private static bool IsMonoRoundKnockout(Stage stage) =>
        stage is { IsSwiss: false, Rounds.Count: 1, Groups.Count: 0, Matchdays.Count: 0 };
}
