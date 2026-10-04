// -----------------------------------------------------------------------
// <copyright file="DematerializationPreflight.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Command preflight for dematerialization (delegates to <see cref="DematerializationCapability"/>).
/// </summary>
internal static class DematerializationPreflight
{
    public static void EnsureNoOrphanMatches(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        var reason = DematerializationCapability.TryOrphanReason(
            stage,
            [.. stageMatches.Select(DematerializationMatchGate.FromMatch)]);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }

    public static void EnsureNonePlayed(IReadOnlyList<Match> matchesInScope)
    {
        ArgumentNullException.ThrowIfNull(matchesInScope);

        var reason = DematerializationCapability.TryMatchLifecycleReason(
            [.. matchesInScope.Select(DematerializationMatchGate.FromMatch)]);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }

    public static void EnsureCupMutable(Competition competition, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        var format = StructureSkeleton.InferFormat(stage);
        if (format is not StructureFormatKind.Cup)
        {
            throw new ApplicationFailureException(
                "Dematerialization for this stage requires Cup structure.",
                ApplicationErrorCodes.DematerializationFailure);
        }

        var reason = DematerializationCapability.TryFullPhaseLifecycleReason(
            competition,
            stage,
            StructureFormatKind.Cup);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }

    public static void EnsureChampionshipOrGroupsMutable(Competition competition, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        var format = StructureSkeleton.InferFormat(stage);
        if (format is not (StructureFormatKind.Championship or StructureFormatKind.Groups))
        {
            throw new ApplicationFailureException(
                "Dematerialization for this stage requires Championship or Groups structure.",
                ApplicationErrorCodes.DematerializationFailure);
        }

        var reason = DematerializationCapability.TryFullPhaseLifecycleReason(
            competition,
            stage,
            format.Value);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }

    public static void EnsureSwissDematerializeMutable(Competition competition, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        if (!stage.IsSwiss)
        {
            throw new ApplicationFailureException(
                "Dematerialization for this stage requires Swiss settings.",
                ApplicationErrorCodes.DematerializationFailure);
        }

        var reason = DematerializationCapability.TrySwissLifecycleReason(competition, stage);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }

    public static void EnsureContiguousSwissMatchdays(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        var reason = DematerializationCapability.TrySwissContiguousReason(stage);
        if (reason is not null)
        {
            DematerializationCapability.ThrowForDisabledReason(reason);
        }
    }
}
