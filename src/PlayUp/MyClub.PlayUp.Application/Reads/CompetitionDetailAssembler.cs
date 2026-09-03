// -----------------------------------------------------------------------
// <copyright file="CompetitionDetailAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="CompetitionDetailDto"/> from Competition + loaded Stages.
/// </summary>
public static class CompetitionDetailAssembler
{
    /// <summary>
    /// Maps competition and stages into a product overview (no Domain mutation).
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages referenced by the competition (same order preferred).</param>
    /// <returns>The assembled overview.</returns>
    public static CompetitionDetailDto Assemble(Competition competition, IReadOnlyList<Stage> stages)
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

        return AssembleCore(competition, stageSummaries);
    }

    /// <summary>
    /// Maps competition and projected stage summaries into a product overview.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stageSummaries">Stage summaries in competition order.</param>
    /// <returns>The assembled overview.</returns>
    public static CompetitionDetailDto Assemble(
        Competition competition,
        IReadOnlyList<StageSummaryRow> stageSummaries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stageSummaries);

        if (stageSummaries.Count == competition.StageIds.Count
            && stageSummaries.Select(row => row.Id).SequenceEqual(competition.StageIds))
        {
            return AssembleCore(
                competition,
                [.. stageSummaries.Select(row => new CompetitionStageSummaryDto(row.Id.Value, row.Name, row.Status))]);
        }

        var byId = stageSummaries.ToDictionary(row => row.Id);
        var ordered = new List<CompetitionStageSummaryDto>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            if (!byId.TryGetValue(stageId, out var row))
            {
                throw new ApplicationFailureException(
                    $"Stage '{stageId}' was not found.",
                    ApplicationErrorCodes.StageNotFound);
            }

            ordered.Add(new CompetitionStageSummaryDto(row.Id.Value, row.Name, row.Status));
        }

        return AssembleCore(competition, ordered);

    }

    private static CompetitionDetailDto AssembleCore(
        Competition competition,
        IReadOnlyList<CompetitionStageSummaryDto> stageSummaries)
    {
        var entries = competition.Entries
            .Select(entry => new CompetitionEntrySummaryDto(
                entry.Id.Value,
                entry.DisplayName,
                entry.Status,
                entry.ShortName?.Value,
                entry.LogoMediaId?.Value,
                entry.PrimaryColor?.Value,
                entry.SecondaryColor?.Value))
            .ToArray();

        return new CompetitionDetailDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            entries,
            stageSummaries,
            competition.CompletionMode,
            competition.ShortName?.Value,
            competition.LogoMediaId?.Value,
            competition.ScheduledStart,
            competition.ScheduledEnd);
    }
}
