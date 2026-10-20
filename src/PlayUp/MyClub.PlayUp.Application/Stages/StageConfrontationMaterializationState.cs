// -----------------------------------------------------------------------
// <copyright file="StageConfrontationMaterializationState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared confrontation coverage for Materialize workspace and Start readiness (Ch/G/Cup).
/// </summary>
internal static class StageConfrontationMaterializationState
{
    /// <summary>
    /// Count of confrontations that are determined, not yet materialized, and materializable in Ready.
    /// Swiss and unknown formats return 0 (no Start gate on this predicate).
    /// </summary>
    /// <summary>
    /// Ready Championship / Groups whose structural prerequisites are met but round-robin yields no confrontation.
    /// </summary>
    internal static bool LacksExploitableInitialConfrontations(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is not StageStatus.Ready || !IsChampionshipOrGroupsMutable(competition, stage))
        {
            return false;
        }

        var stages = competitionStages ?? [stage];
        var format = StructureSkeleton.InferFormat(stage);
        return format switch
        {
            StructureFormatKind.Championship when ChampionshipSwissPhasePlaces.IsRosterComplete(
                competition,
                stage,
                stages) => CountChampionshipDeterminedConfrontations(competition, stage, stages) == 0,
            StructureFormatKind.Groups when AreGroupsReady(stage) =>
                CountGroupsDeterminedConfrontations(stage) == 0,
            _ => false
        };
    }

    internal static int CountEligibleUnmaterialized(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        if (stage.Status is not StageStatus.Ready)
        {
            return 0;
        }

        var stages = competitionStages ?? [stage];
        var format = StructureSkeleton.InferFormat(stage);
        return format switch
        {
            StructureFormatKind.Championship => CountChampionshipEligible(competition, stage, stages, matchSummaries),
            StructureFormatKind.Groups => CountGroupsEligible(competition, stage, matchSummaries),
            StructureFormatKind.Cup => CountCupEligible(stage),
            StructureFormatKind.Swiss => 0,
            null => 0,
            _ => 0
        };
    }

    private static int CountChampionshipEligible(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        if (!IsChampionshipOrGroupsMutable(competition, stage))
        {
            return 0;
        }

        if (!ChampionshipSwissPhasePlaces.IsRosterComplete(competition, stage, competitionStages))
        {
            return 0;
        }

        var entries = ChampionshipSwissPhasePlaces.ResolveCompositionRoster(competition, stage);
        if (entries.Count < 2)
        {
            return 0;
        }

        var determined = CountChampionshipDeterminedConfrontations(competition, stage, competitionStages);
        if (determined == 0)
        {
            return 0;
        }

        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);
        var pairs = ChampionshipDeterminedPairs(entries, stage.MatchGenerationFormat);
        var materialized = pairs.Count(covered.Contains);
        return determined - materialized;
    }

    private static int CountChampionshipDeterminedConfrontations(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        if (!ChampionshipSwissPhasePlaces.IsRosterComplete(competition, stage, competitionStages))
        {
            return 0;
        }

        var entries = ChampionshipSwissPhasePlaces.ResolveCompositionRoster(competition, stage);
        return ChampionshipDeterminedPairs(entries, stage.MatchGenerationFormat).Length;
    }

    private static (EntryId Home, EntryId Away)[] ChampionshipDeterminedPairs(
        IReadOnlyList<EntryId> entries,
        MatchGenerationFormat format) =>
        entries.Count < 2
            ? []
            :
            [
                .. MaterializeMatches.BuildRoundRobinRounds(entries, format)
                    .SelectMany(round => round)
            ];

    private static int CountGroupsEligible(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        if (!IsChampionshipOrGroupsMutable(competition, stage))
        {
            return 0;
        }

        if (!AreGroupsReady(stage))
        {
            return 0;
        }

        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);
        var determined = CountGroupsDeterminedConfrontations(stage);
        return determined == 0
            ? 0
            : (from @group in stage.Groups
               where @group.EntryIds.Count >= 2
               select MaterializeMatches.BuildRoundRobinRounds([.. @group.EntryIds], stage.MatchGenerationFormat)
                    .SelectMany(round => round)
               into pairs
               select pairs.Count(pair => !covered.Contains(pair))).Sum();
    }

    private static int CountGroupsDeterminedConfrontations(Stage stage) =>
        stage.Groups
            .Where(group => group.EntryIds.Count >= 2)
            .Sum(group => MaterializeMatches.BuildRoundRobinRounds(
                    [.. group.EntryIds],
                    stage.MatchGenerationFormat)
                .Sum(round => round.Count));

    private static bool AreGroupsReady(Stage stage) =>
        stage.Groups.Count > 0 && stage.Groups.All(group => group.EntryIds.Count >= 2);

    private static int CountCupEligible(Stage stage)
    {
        if (stage.BracketPairs.Count == 0)
        {
            return 0;
        }

        var eligible = 0;
        foreach (var pair in stage.BracketPairs)
        {
            if (CupBracketPairMaterialization.IsEligibleForMaterialization(stage, pair))
            {
                eligible++;
            }
        }

        return eligible;
    }

    private static HashSet<(EntryId Home, EntryId Away)> CoveredDirectedPairs(
        StageId stageId,
        IReadOnlyList<MatchSummaryRow> matchSummaries) =>
    [
        .. matchSummaries
            .Where(row => row.StageId.Equals(stageId))
            .Select(row => (row.HomeEntryId, row.AwayEntryId))
    ];

    private static bool IsChampionshipOrGroupsMutable(Competition competition, Stage stage) =>
        competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready
        && stage.Status is StageStatus.Ready;
}
