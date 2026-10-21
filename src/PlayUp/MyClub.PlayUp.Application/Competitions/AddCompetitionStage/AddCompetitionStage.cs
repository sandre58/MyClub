// -----------------------------------------------------------------------
// <copyright file="AddCompetitionStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: atomic birth of a competition stage (identity + skeleton).
/// </summary>
/// <remarks>
/// Single unit of work: create stage, attach, materialize form.
/// No DrawRules seed, no relations, no population. Failure of any step → caller does not SaveChanges.
/// </remarks>
public static class AddCompetitionStage
{
    /// <summary>
    /// Creates a Draft stage with the given identity + skeleton intent and attaches it.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="intent">Validated format intent (name + kind + skeleton params).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The new structured stage (caller must persist in one SaveChanges).</returns>
    public static Stage Execute(Competition competition, StructureIntent intent, IClock clock)
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
                $"Stages cannot be added while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        var isClassifying = intent.Format is not StructureFormatKind.Cup;
        var stage = Stage.Create(
            competition.Id,
            new StageName(intent.StageName),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifying),
            DefaultsBinding.AllBound(isClassifying),
            clock);
        competition.AddStage(stage.Id, clock);
        StructureSkeleton.Apply(stage, intent, competition.Regulation.StandingRules, clock);
        return stage;
    }
}
