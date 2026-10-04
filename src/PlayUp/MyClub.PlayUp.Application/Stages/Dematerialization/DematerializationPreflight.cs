// -----------------------------------------------------------------------
// <copyright file="DematerializationPreflight.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared dematerialization preflight (orphans, played gate) for format-specific use cases.
/// </summary>
internal static class DematerializationPreflight
{
    /// <summary>
    /// Ensures no persisted match on the stage lacks a fixture attachment.
    /// </summary>
    public static void EnsureNoOrphanMatches(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        var orphan = stageMatches.FirstOrDefault(match =>
            match.StageId.Equals(stage.Id) && !stage.HasMatch(match.Id));
        if (orphan is not null)
        {
            throw new ApplicationFailureException(
                $"Match '{orphan.Id}' is persisted for the stage but not attached to a fixture.",
                ApplicationErrorCodes.MaterializationStateInconsistent);
        }
    }

    /// <summary>
    /// Refuses dematerialization when any match in scope was already played (V1 gate).
    /// </summary>
    public static void EnsureNonePlayed(IReadOnlyList<Match> matchesInScope)
    {
        ArgumentNullException.ThrowIfNull(matchesInScope);

        foreach (var match in matchesInScope)
        {
            if (BlocksDematerialization(match))
            {
                throw new ApplicationFailureException(
                    $"Dematerialization is blocked because match '{match.Id}' was already played.",
                    ApplicationErrorCodes.DematerializationBlocked);
            }
        }
    }

    /// <summary>
    /// Cup lifecycle gates (mirror <see cref="MaterializeCupFromOccupiedSlots"/>).
    /// </summary>
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

        if (stage.Rounds.Count == 0 || stage.Matchdays.Count > 0 || stage.Groups.Count > 0)
        {
            throw new ApplicationFailureException(
                "Dematerialization for this stage requires a Cup stage (rounds only).",
                ApplicationErrorCodes.DematerializationFailure);
        }

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Dematerialization is not allowed while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Dematerialization is not allowed while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    /// <summary>
    /// Championship / Groups lifecycle gates (mirror <see cref="MaterializeMatches"/>).
    /// </summary>
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

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Dematerialization is not allowed while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Dematerialization is not allowed while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    internal static bool BlocksDematerialization(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return match.Status is MatchStatus.Live or MatchStatus.Finished
               || (match.Status is MatchStatus.Cancelled && match.HasObservedLive);
    }

    /// <summary>
    /// Swiss dematerialization gates (mirror <see cref="GenerateNextRound"/>).
    /// </summary>
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

        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Dematerialization is not allowed while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (stage.Status is not StageStatus.Running)
        {
            throw new ApplicationFailureException(
                $"Swiss dematerialization requires stage status Running (current: '{stage.Status}').",
                ApplicationErrorCodes.DematerializationFailure);
        }
    }

    /// <summary>
    /// Swiss rounds must be a contiguous 1..N sequence (no gaps).
    /// </summary>
    public static void EnsureContiguousSwissMatchdays(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (stage.Matchdays.Count == 0)
        {
            return;
        }

        var numbers = stage.Matchdays.Select(matchday => matchday.Number).Order().ToArray();
        for (var index = 0; index < numbers.Length; index++)
        {
            if (numbers[index] != index + 1)
            {
                throw new ApplicationFailureException(
                    "Swiss matchday numbering is not contiguous; dematerialization is blocked.",
                    ApplicationErrorCodes.MaterializationStateInconsistent);
            }
        }
    }
}
