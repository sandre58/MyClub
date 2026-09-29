// -----------------------------------------------------------------------
// <copyright file="ConfigureStructure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: configure primary Stage structure from a typed format intent.
/// </summary>
/// <remarks>
/// Orchestrates Domain APIs only — not a generic Stage builder. Atomicity is the caller's
/// single SaveChanges (Stage create + Competition.AddStage + structure in one unit of work).
/// Does not generate Draw, Schedule, or Matches. Does not seed DrawRules.
/// Rebuild of an already-structured primary stage is same-kind only.
/// </remarks>
public static class ConfigureStructure
{
    /// <summary>
    /// Ensures a primary stage exists, clears prior structure, and builds the intent skeleton.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="primaryStage">Existing first stage when present; otherwise null.</param>
    /// <param name="intent">Typed format intent.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Stage (possibly new) ready for persistence.</returns>
    public static ConfigureStructureResult Execute(
        Competition competition,
        Stage? primaryStage,
        StructureIntent intent,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Structure cannot be configured while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        var stageCreated = false;
        StructureRebuildImpact? rebuildImpact = null;
        Stage stage;
        if (primaryStage is null)
        {
            if (competition.StageIds.Count > 0)
            {
                throw new ApplicationFailureException(
                    "Competition references stages that were not loaded for ConfigureStructure.",
                    ApplicationErrorCodes.StageNotFound);
            }

            var isClassifying = intent.Format is not StructureFormatKind.Cup;
            stage = Stage.Create(
                competition.Id,
                new StageName(intent.StageName),
                StageRegulation.MaterializeFrom(competition.Regulation, isClassifying),
                DefaultsBinding.AllBound(isClassifying),
                clock);
            competition.AddStage(stage.Id, clock);
            stageCreated = true;
        }
        else
        {
            if (competition.StageIds.Count == 0 || !competition.StageIds[0].Equals(primaryStage.Id))
            {
                throw new ApplicationFailureException(
                    $"Stage '{primaryStage.Id}' is not the primary stage of competition '{competition.Id}'.",
                    ApplicationErrorCodes.StageNotInCompetition);
            }

            if (StructureSkeleton.CountAttachedMatches(primaryStage) > 0)
            {
                throw new ApplicationFailureException(
                    $"Stage '{primaryStage.Id}' cannot be rebuilt while matches are attached.",
                    ApplicationErrorCodes.StructureNotMutable);
            }

            StructureSkeleton.EnsureSameKind(primaryStage, intent.Format);

            stage = primaryStage;
            if (!string.Equals(stage.Name.Value, intent.StageName, StringComparison.Ordinal))
            {
                stage.Rename(new StageName(intent.StageName));
            }

            rebuildImpact = StructureSkeleton.Clear(stage, clock);
        }

        StructureSkeleton.Apply(stage, intent, competition.Regulation.StandingRules, clock);
        return new ConfigureStructureResult(stage, stageCreated, rebuildImpact);
    }
}
