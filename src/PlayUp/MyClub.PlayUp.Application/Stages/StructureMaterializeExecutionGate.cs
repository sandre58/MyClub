// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeExecutionGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// G-MAT1: refuse match production when a necessary confrontation cannot always decide a draw
/// (same predicate as <see cref="CompetitionFlowAnalyzer.AnalyzeExecution"/> for the target stage).
/// </summary>
internal static class StructureMaterializeExecutionGate
{
    internal static void EnsureCupStage(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var undecidable = CompetitionFlowAnalyzer.AnalyzeExecution([stage])
            .Where(diagnostic =>
                diagnostic.Code == CompetitionFlowAnalyzer.ConfrontationUndecidable
                && diagnostic.StageId == stage.Id.Value)
            .Select(diagnostic => diagnostic.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (undecidable.Length == 0)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{stage.Id}' materialization refused: {string.Join(", ", undecidable)}.",
            ApplicationErrorCodes.ConfrontationUndecidable);
    }
}
