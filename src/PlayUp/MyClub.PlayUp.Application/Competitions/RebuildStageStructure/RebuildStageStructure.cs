// -----------------------------------------------------------------------
// <copyright file="RebuildStageStructure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: rebuild a stage skeleton (same format kind only).
/// </summary>
public static class RebuildStageStructure
{
    /// <summary>
    /// Clears and rematerializes the stage form from <paramref name="intent"/>.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="stage">Target stage (must belong to the competition).</param>
    /// <param name="intent">Same-kind skeleton intent.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Cleared topology impact.</returns>
    public static StructureRebuildImpact Execute(
        Competition competition,
        Stage stage,
        StructureIntent intent,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Structure cannot be rebuilt while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (!competition.StageIds.Contains(stage.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' is not part of competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        if (StructureSkeleton.CountAttachedMatches(stage) > 0)
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' cannot be rebuilt while matches are attached.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        StructureSkeleton.EnsureSameKind(stage, intent.Format);

        if (!string.Equals(stage.Name.Value, intent.StageName, StringComparison.Ordinal))
        {
            stage.Rename(new StageName(intent.StageName));
        }

        var impact = StructureSkeleton.Clear(stage, clock);
        StructureSkeleton.Apply(stage, intent, competition.Regulation.StandingRules, clock);
        return impact;
    }
}
