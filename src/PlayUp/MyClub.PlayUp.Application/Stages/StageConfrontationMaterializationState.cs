// -----------------------------------------------------------------------
// <copyright file="StageConfrontationMaterializationState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
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
    internal static int CountEligibleUnmaterialized(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        if (stage.Status is not StageStatus.Ready)
        {
            return 0;
        }

        var format = StructureSkeleton.InferFormat(stage);
        return format switch
        {
            StructureFormatKind.Championship => CountChampionshipEligible(competition, stage, matchSummaries),
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
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        if (!IsChampionshipOrGroupsMutable(competition, stage))
        {
            return 0;
        }

        var entries = ActiveEntries(competition);
        if (entries.Count < 2)
        {
            return 0;
        }

        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);
        var pairs = MaterializeMatches.BuildRoundRobinRounds(entries, stage.MatchGenerationFormat)
            .SelectMany(round => round)
            .ToArray();
        var determined = pairs.Length;
        if (determined == 0)
        {
            return 0;
        }

        var materialized = pairs.Count(covered.Contains);
        return determined - materialized;
    }

    private static int CountGroupsEligible(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        if (!IsChampionshipOrGroupsMutable(competition, stage))
        {
            return 0;
        }

        var groupsReady = stage.Groups.Count > 0
                          && stage.Groups.All(group => group.EntryIds.Count >= 2);
        if (!groupsReady)
        {
            return 0;
        }

        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);

        return (from @group in stage.Groups
            where @group.EntryIds.Count >= 2
            select MaterializeMatches.BuildRoundRobinRounds([.. @group.EntryIds], stage.MatchGenerationFormat)
                .SelectMany(round => round)
            into pairs
            select pairs.Count(pair => !covered.Contains(pair))).Sum();
    }

    private static int CountCupEligible(Stage stage)
    {
        if (stage.BracketPairs.Count == 0)
        {
            return 0;
        }

        var eligible = 0;
        foreach (var pair in stage.BracketPairs)
        {
            var slotA = stage.FindSlot(pair.SlotAKey);
            var slotB = stage.FindSlot(pair.SlotBKey);
            var determined = slotA?.EntryId is { } home
                             && slotB?.EntryId is { } away
                             && !home.Equals(away);
            var materialized = stage.FindFixtureByBracketPairKey(pair.PairKey) is not null;
            if (determined && !materialized)
            {
                eligible++;
            }
        }

        return eligible;
    }

    private static HashSet<(EntryId Home, EntryId Away)> CoveredDirectedPairs(
        StageId stageId,
        IReadOnlyList<MatchSummaryRow> matchSummaries) =>
        [.. matchSummaries
            .Where(row => row.StageId.Equals(stageId))
            .Select(row => (row.HomeEntryId, row.AwayEntryId))];

    private static IReadOnlyList<EntryId> ActiveEntries(Competition competition) =>
    [
        .. competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .Select(entry => entry.Id)
            .OrderBy(id => id.Value)
    ];

    private static bool IsChampionshipOrGroupsMutable(Competition competition, Stage stage) =>
        competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready
        && stage.Status is StageStatus.Ready;
}
