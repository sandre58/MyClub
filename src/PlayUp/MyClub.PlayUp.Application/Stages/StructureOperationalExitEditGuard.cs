// -----------------------------------------------------------------------
// <copyright file="StructureOperationalExitEditGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// G-OPS1: allow qualification/progression exit edits on Draft/Ready upstream when no production is attached (D2).
/// </summary>
internal static class StructureOperationalExitEditGuard
{
    internal static void Ensure(Stage upstream, IReadOnlyList<Match>? upstreamMatches, string ruleKind)
    {
        ArgumentNullException.ThrowIfNull(upstream);

        if (upstream.Status is StageStatus.Draft)
        {
            return;
        }

        if (upstream.Status is not StageStatus.Ready)
        {
            throw new ApplicationFailureException(
                $"{ruleKind} cannot be replaced while stage status is '{upstream.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (upstreamMatches is not null
            && StageAttachedProduction.HasAttachedMatches(upstream, upstreamMatches)
            && !AllowsOperationalEditsDespiteAttachedProduction(upstream))
        {
            throw new ApplicationFailureException(
                $"{ruleKind} cannot be replaced while matches are attached to stage '{upstream.Id}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    /// <summary>
    /// Cup mono-round: incomplete canonical entry round may still be completed while partial production exists (C1 recette).
    /// </summary>
    private static bool AllowsOperationalEditsDespiteAttachedProduction(Stage stage) =>
        StructureSkeleton.InferFormat(stage) == StructureFormatKind.Cup
        && CupCanonicalEntryRoundStartGate.BlocksStart(stage);
}
