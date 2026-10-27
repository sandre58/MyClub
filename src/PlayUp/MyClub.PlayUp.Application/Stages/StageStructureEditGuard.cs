// -----------------------------------------------------------------------
// <copyright file="StageStructureEditGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application guard for heavy structure edits.
/// The stage must be <see cref="StageStatus.Draft"/>.
/// While the competition is <see cref="CompetitionStatus.Running"/> or <see cref="CompetitionStatus.Suspended"/>,
/// a Draft phase keeps operational authoring and denies form rebuild and structural adds.
/// </summary>
internal static class StageStructureEditGuard
{
    internal static void EnsureEditable(Stage stage, Competition competition)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        if (stage.Status is not StageStatus.Draft)
        {
            throw new ApplicationFailureException(
                $"Structure cannot be modified while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        EnsureCompetitionAllowsHeavyStructure(competition);
    }

    /// <summary>
    /// Denies heavy structure on a Draft stage while the competition is Running or Suspended.
    /// No-op when the stage is not Draft — later statuses stay with the domain
    /// (standing rules after Start, for example).
    /// </summary>
    internal static void EnsureDraftHeavyStructureAllowed(Stage stage, Competition competition)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        if (stage.Status is not StageStatus.Draft)
        {
            return;
        }

        EnsureCompetitionAllowsHeavyStructure(competition);
    }

    private static void EnsureCompetitionAllowsHeavyStructure(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended)
        {
            throw new ApplicationFailureException(
                $"Structure cannot be modified while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
