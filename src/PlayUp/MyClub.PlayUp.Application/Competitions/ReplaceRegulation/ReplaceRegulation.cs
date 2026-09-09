// -----------------------------------------------------------------------
// <copyright file="ReplaceRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: replace competition regulation as a whole, then propagate bound defaults to stages.
/// </summary>
/// <remarks>
/// Distinct from <see cref="BootstrapRegulation"/> (Create default only). Ready demotes to Draft via Domain.
/// Competition replace + stage propagations are intended to be persisted in one SaveChanges by the caller.
/// </remarks>
public static class ReplaceRegulation
{
    /// <summary>
    /// Replaces the competition regulation and propagates bound Match/Standing parts to eligible stages.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="stages">Competition stages loaded for update (may be empty).</param>
    /// <param name="regulation">New regulation (copied by Domain).</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(
        Competition competition,
        IReadOnlyList<Stage> stages,
        Regulation regulation,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);

        competition.ReplaceRegulation(regulation, clock);

        foreach (var stage in stages)
        {
            if (!competition.HasStage(stage.Id))
            {
                throw new ApplicationFailureException(
                    $"Stage '{stage.Id}' is not part of competition '{competition.Id}'.",
                    ApplicationErrorCodes.StageNotInCompetition);
            }

            stage.PropagateBoundDefaults(competition.Regulation, clock);
        }
    }
}
