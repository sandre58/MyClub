// -----------------------------------------------------------------------
// <copyright file="CompetitionOverviewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="CompetitionOverviewDto"/> from Competition + loaded Stages.
/// </summary>
public static class CompetitionOverviewAssembler
{
    /// <summary>
    /// Maps competition and stages into a product overview (no Domain mutation).
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages referenced by the competition (same order preferred).</param>
    /// <returns>The assembled overview.</returns>
    public static CompetitionOverviewDto Assemble(Competition competition, IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        var byId = stages.ToDictionary(stage => stage.Id);
        var stageSummaries = new List<CompetitionStageSummaryDto>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            if (!byId.TryGetValue(stageId, out var stage))
            {
                throw new ApplicationFailureException(
                    $"Stage '{stageId}' was not found.",
                    ApplicationErrorCodes.StageNotFound);
            }

            stageSummaries.Add(new CompetitionStageSummaryDto(stage.Id.Value, stage.Name.Value, stage.Status));
        }

        var entries = competition.Entries
            .Select(entry => new CompetitionEntrySummaryDto(entry.Id.Value, entry.DisplayName, entry.Status))
            .ToArray();

        return new CompetitionOverviewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            entries,
            stageSummaries);
    }
}
