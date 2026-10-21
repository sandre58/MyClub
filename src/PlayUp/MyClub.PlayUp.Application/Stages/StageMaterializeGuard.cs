// -----------------------------------------------------------------------
// <copyright file="StageMaterializeGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application guard: match materialization is allowed in <see cref="StageStatus.Ready"/> only.
/// </summary>
internal static class StageMaterializeGuard
{
    internal static void EnsureStageReadyForMaterialize(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is not StageStatus.Ready)
        {
            throw new ApplicationFailureException(
                $"Matches can only be materialized while stage status is Ready (current: '{stage.Status}').",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    internal static void EnsureChampionshipOrGroupsCompetition(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    internal static void EnsureCupCompetition(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);

        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
