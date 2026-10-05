// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Per-stage Structure workspace projection for Materialize (counts + exhaustive rows).
/// </summary>
internal static class StructureMaterializeAssembler
{
    public const string LabelVariantExhausted = "exhausted";
    public const string LabelVariantIdle = "idle";

    /// <summary>
    /// Builds the workspace capability, or <see langword="null"/> when the format has no Materialize command.
    /// </summary>
    public static StructureMaterializeDto? Assemble(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        var format = StructureSkeleton.InferFormat(stage);
        return format switch
        {
            StructureFormatKind.Championship => AssembleChampionship(competition, stage, matchSummaries),
            StructureFormatKind.Groups => AssembleGroups(competition, stage, matchSummaries),
            StructureFormatKind.Cup => AssembleCup(competition, stage),
            StructureFormatKind.Swiss => null,
            null => null,
            _ => null
        };
    }

    private static StructureMaterializeDto AssembleChampionship(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        var entries = ActiveEntries(competition);
        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);
        var pairs = entries.Count < 2
            ? []
            : MaterializeMatches.BuildRoundRobinRounds(entries, stage.MatchGenerationFormat)
                .SelectMany(round => round)
                .ToArray();
        var determined = pairs.Length;
        var materialized = pairs.Count(covered.Contains);

        var mutable = IsChampionshipOrGroupsMutable(competition, stage);
        var eligible = mutable && determined > 0 ? determined - materialized : 0;
        return Pack(
            competition,
            stage,
            StructureFormatKind.Championship,
            OverviewAssembler.ActionMaterializeMatches,
            determined,
            eligible,
            materialized,
            commandEnabled: eligible > 0,
            groupsReady: true);
    }

    private static StructureMaterializeDto AssembleGroups(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        var covered = CoveredDirectedPairs(stage.Id, matchSummaries);
        var names = EntryDisplayNames.ToMap(competition);
        var groupsReady = stage.Groups.Count > 0
                          && stage.Groups.All(group => group.EntryIds.Count >= 2);
        var mutable = IsChampionshipOrGroupsMutable(competition, stage);
        var rows = new List<StructureMaterializeConfrontationDto>();
        foreach (var group in stage.Groups)
        {
            if (group.EntryIds.Count < 2)
            {
                continue;
            }

            var pairs = MaterializeMatches.BuildRoundRobinRounds(
                    [.. group.EntryIds],
                    stage.MatchGenerationFormat)
                .SelectMany(round => round);
            foreach (var (home, away) in pairs)
            {
                var materialized = covered.Contains((home, away));
                var eligible = groupsReady && mutable && !materialized;
                rows.Add(
                    new StructureMaterializeConfrontationDto(
                        $"{home.Value:N}|{away.Value:N}",
                        EntryDisplayNames.Resolve(names, home) ?? home.Value.ToString(),
                        EntryDisplayNames.Resolve(names, away) ?? away.Value.ToString(),
                        Determined: true,
                        materialized,
                        eligible,
                        Selectable: false,
                        group.Name));
            }
        }

        var determined = rows.Count;
        var materializedCount = rows.Count(row => row.Materialized);
        var eligibleCount = rows.Count(row => row.Eligible);
        return Pack(
            competition,
            stage,
            StructureFormatKind.Groups,
            OverviewAssembler.ActionMaterializeMatches,
            determined,
            eligibleCount,
            materializedCount,
            commandEnabled: eligibleCount > 0,
            groupsReady,
            rows);
    }

    private static StructureMaterializeDto AssembleCup(Competition competition, Stage stage)
    {
        var names = EntryDisplayNames.ToMap(competition);
        var mutable = IsCupMutable(competition, stage);
        var rows = new List<StructureMaterializeConfrontationDto>(stage.BracketPairs.Count);
        foreach (var pair in stage.BracketPairs.OrderBy(item => item.PairKey, StringComparer.Ordinal))
        {
            var slotA = stage.FindSlot(pair.SlotAKey);
            var slotB = stage.FindSlot(pair.SlotBKey);
            var determined = slotA?.EntryId is { } home
                             && slotB?.EntryId is { } away
                             && !home.Equals(away);
            var materialized = stage.FindFixtureByBracketPairKey(pair.PairKey) is not null;
            var eligible = determined && !materialized && mutable;
            rows.Add(
                new StructureMaterializeConfrontationDto(
                    pair.PairKey,
                    SideLabel(slotA, names, pair.SlotAKey),
                    SideLabel(slotB, names, pair.SlotBKey),
                    determined,
                    materialized,
                    eligible,
                    Selectable: eligible));
        }

        var determinedCount = rows.Count(row => row.Determined);
        var materializedCount = rows.Count(row => row.Materialized);
        var eligibleCount = rows.Count(row => row.Eligible);
        return Pack(
            competition,
            stage,
            StructureFormatKind.Cup,
            OverviewAssembler.ActionMaterializeFromOccupiedSlots,
            determinedCount,
            eligibleCount,
            materializedCount,
            commandEnabled: eligibleCount > 0,
            groupsReady: true,
            rows);
    }

    private static StructureMaterializeDto Pack(
        Competition competition,
        Stage stage,
        StructureFormatKind format,
        string commandCode,
        int determined,
        int eligible,
        int materialized,
        bool commandEnabled,
        bool groupsReady,
        IReadOnlyList<StructureMaterializeConfrontationDto>? confrontations = null)
    {
        var disabledReason = ResolveDisabledReason(
            competition,
            stage,
            format,
            determined,
            eligible,
            materialized,
            commandEnabled,
            groupsReady);
        return new StructureMaterializeDto(
            commandCode,
            LabelVariant(determined, eligible, materialized),
            determined,
            eligible,
            materialized,
            commandEnabled,
            confrontations ?? [],
            disabledReason);
    }

    private static string? ResolveDisabledReason(
        Competition competition,
        Stage stage,
        StructureFormatKind format,
        int determined,
        int eligible,
        int materialized,
        bool commandEnabled,
        bool groupsReady)
    {
        if (commandEnabled)
        {
            return null;
        }

        if (determined > 0 && materialized >= determined)
        {
            return null;
        }

        var mutable = format switch
        {
            StructureFormatKind.Cup => IsCupMutable(competition, stage),
            StructureFormatKind.Groups or StructureFormatKind.Championship => IsChampionshipOrGroupsMutable(
                competition,
                stage),
            StructureFormatKind.Swiss => false,
            _ => false
        };

        return !mutable
            ? StructureMaterializeDisabledReasons.StructureNotMutable
            : determined == 0
            ? StructureMaterializeDisabledReasons.NoDeterminedConfrontations
            : format == StructureFormatKind.Groups && !groupsReady
            ? StructureMaterializeDisabledReasons.GroupsNotReady
            : eligible == 0 ? StructureMaterializeDisabledReasons.NoEligibleConfrontations : null;
    }

    private static string LabelVariant(int determined, int eligible, int materialized) =>
        eligible > 0 && materialized > 0
            ? OverviewAssembler.LabelVariantComplete
            : eligible > 0
                ? OverviewAssembler.LabelVariantCreate
                : determined > 0
                    ? LabelVariantExhausted
                    : LabelVariantIdle;

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

    private static string SideLabel(
        Slot? slot,
        IReadOnlyDictionary<EntryId, string> names,
        string slotKey) =>
        slot?.EntryId is { } entryId
            ? EntryDisplayNames.Resolve(names, entryId) ?? slotKey
            : slotKey;

    /// <summary>
    /// Read-side eligibility mirrors command preflight in
    /// <see cref="MaterializeMatches"/> (Ch/G) and <see cref="MaterializeCupFromOccupiedSlots"/> (Cup).
    /// Cup allows a Running competition; Ch/G require Draft/Ready. Dematerialization uses separate rules in
    /// <see cref="DematerializationCapability"/> — intentionally not unified here.
    /// </summary>
    private static bool IsChampionshipOrGroupsMutable(Competition competition, Stage stage) =>
        competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready
        && stage.Status is StageStatus.Draft or StageStatus.Ready;

    private static bool IsCupMutable(Competition competition, Stage stage) =>
        competition.Status is not (CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        && stage.Status is StageStatus.Draft or StageStatus.Ready;
}
