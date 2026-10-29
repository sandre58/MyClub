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
/// Allows qualification and progression exit edits on Draft or Ready upstream when no production is attached.
/// </summary>
internal static class StructureOperationalExitEditGuard
{
    /// <summary>
    /// Rejects exit-rule edits when stage status alone forbids them, without loading matches.
    /// </summary>
    internal static void EnsureStatusAllowsExitEditWithoutMatches(Stage upstream, string ruleKind)
    {
        ArgumentNullException.ThrowIfNull(upstream);

        if (upstream.Status is StageStatus.Draft or StageStatus.Ready)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{ruleKind} cannot be replaced while stage status is '{upstream.Status}'.",
            ApplicationErrorCodes.StructureNotMutable);
    }

    internal static void Ensure(Stage upstream, IReadOnlyList<Match>? upstreamMatches, string ruleKind)
    {
        ArgumentNullException.ThrowIfNull(upstream);

        var attached = upstreamMatches is not null
            && StageAttachedProduction.HasAttachedMatches(upstream, upstreamMatches);
        if (DenialCode(upstream, attached) is not { } code)
        {
            return;
        }

        var message = upstream.Status is not StageStatus.Ready
            ? $"{ruleKind} cannot be replaced while stage status is '{upstream.Status}'."
            : $"{ruleKind} cannot be replaced while matches are attached to stage '{upstream.Id}'.";
        throw new ApplicationFailureException(message, code);
    }

    /// <summary>
    /// Same predicate as <see cref="Ensure"/>, as a block code for draw-action projection.
    /// </summary>
    /// <returns><see cref="ApplicationErrorCodes.StructureNotMutable"/> or <see langword="null"/>.</returns>
    internal static string? DenialCode(Stage stage, bool hasAttachedMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is StageStatus.Draft)
        {
            return null;
        }

        if (stage.Status is not StageStatus.Ready)
        {
            return ApplicationErrorCodes.StructureNotMutable;
        }

        return hasAttachedMatches && !AllowsOperationalEditsDespiteAttachedProduction(stage)
            ? ApplicationErrorCodes.StructureNotMutable
            : null;
    }

    /// <summary>
    /// Cup mono-round: incomplete canonical entry round may still be completed while partial production exists.
    /// </summary>
    private static bool AllowsOperationalEditsDespiteAttachedProduction(Stage stage) =>
        StructureSkeleton.InferFormat(stage) == StructureFormatKind.Cup
        && CupCanonicalEntryRoundStartGate.BlocksStart(stage);
}
