// -----------------------------------------------------------------------
// <copyright file="StructureQualProgHubIntentsMapper.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Hub read projection: qualification / progression authoring is always expressed as intents.
/// Legacy path-list rules are synthesized on read when domain intents are empty.
/// </summary>
internal static class StructureQualProgHubIntentsMapper
{
    internal static IReadOnlyList<StructureQualificationIntentDto>? MapQualificationIntents(
        Stage stage,
        QualificationRules? rules) =>
        rules is null || rules.Paths.Count == 0 ? null : rules.Intents.Count > 0 ? [.. rules.Intents.Select(intent => MapQualificationIntent(stage, intent))] : [.. rules.Paths.Select(path => MapLegacyQualificationPath(stage, path))];

    internal static IReadOnlyList<StructureProgressionIntentDto>? MapProgressionIntents(
        Stage stage,
        ProgressionRules? rules) =>
        rules is null || rules.Paths.Count == 0
            ? null
            : rules.Intents.Count > 0 ? MapPersistedProgressionIntents(stage, rules) : MapLegacyProgressionPaths(stage, rules);

    private static StructureQualificationIntentDto MapQualificationIntent(
        Stage stage,
        QualificationIntent intent)
    {
        string? groupName = null;
        if (intent.GroupId is { } groupId)
        {
            groupName = stage.Groups.FirstOrDefault(g => g.Id.Equals(groupId))?.Name;
        }

        var destinationCount = CountIntentDestinations(intent, stage.Groups.Count);
        return new StructureQualificationIntentDto(
            intent.Id.Value,
            intent.Order,
            intent.SourceKind,
            intent.PositionFrom,
            intent.PositionTo,
            intent.DestinationStageId.Value,
            intent.GroupId?.Value,
            groupName,
            intent.AcrossGroupsPosition,
            intent.Condition?.MinimumPoints,
            destinationCount,
            intent.DestinationSlotKeys,
            intent.DestinationGroupIds.Count == 0 ? null : intent.DestinationGroupIds.Select(g => g.Value).ToArray(),
            intent.DestinationForm);
    }

    private static StructureQualificationIntentDto MapLegacyQualificationPath(
        Stage stage,
        QualificationPath path)
    {
        if (path.Selection.Mode == SelectionMode.Position)
        {
            var intent = QualificationPathExpander.ToSingletonIntent(path, IntentId.New());
            return MapQualificationIntent(stage, intent);
        }

        var (from, to) = path.Selection.Mode switch
        {
            SelectionMode.Range => (path.Selection.Value, path.Selection.EndValue ?? path.Selection.Value),
            _ => (1, Math.Max(1, path.Selection.Value))
        };

        var sourceKind = path.Source.Scope switch
        {
            RankingScope.Group => QualificationIntentSourceKind.SingleGroup,
            RankingScope.AcrossGroups => QualificationIntentSourceKind.AcrossGroups,
            _ => QualificationIntentSourceKind.Overall
        };

        string? groupName = null;
        if (path.Source.GroupId is { } groupId)
        {
            groupName = stage.Groups.FirstOrDefault(g => g.Id.Equals(groupId))?.Name;
        }

        var destinationCount = StructurePopulationVolume.QualificationPathVolume(path);
        IReadOnlyList<string>? slotKeys = path.Destination.TargetsSlot
            ? [path.Destination.SlotKey!]
            : null;
        IReadOnlyList<Guid>? groupIds = path.Destination.TargetsGroup
            ? [path.Destination.GroupId!.Value.Value]
            : null;
        return new StructureQualificationIntentDto(
            LegacyIntentId(stage.Id.Value, "qual", path.Order),
            path.Order,
            sourceKind,
            from,
            to,
            path.Destination.StageId.Value,
            path.Source.GroupId?.Value,
            groupName,
            path.Source.AcrossGroupsPosition,
            path.Condition?.MinimumPoints,
            destinationCount,
            slotKeys,
            groupIds,
            path.Destination.TargetsForm);
    }

    private static IReadOnlyList<StructureProgressionIntentDto> MapPersistedProgressionIntents(
        Stage stage,
        ProgressionRules rules)
    {
        var roundNameById = stage.Rounds.ToDictionary(r => r.Id, r => r.Name);
        var expandCount = stage.BracketPairs.Count > 0
            ? stage.BracketPairs.Count
            : 0;
        return
        [
            .. rules.Intents.Select(intent =>
            {
                var count = expandCount > 0
                    ? expandCount
                    : stage.Rounds
                        .FirstOrDefault(r => r.Id.Equals(intent.RoundId))
                        ?.Fixtures.Count ?? 0;
                return new StructureProgressionIntentDto(
                    intent.Id.Value,
                    intent.Order,
                    intent.RoundId.Value,
                    roundNameById.GetValueOrDefault(intent.RoundId),
                    intent.Outcome,
                    intent.DestinationStageId.Value,
                    intent.DestinationSlotKeys,
                    count,
                    intent.DestinationGroupIds.Count == 0 ? null : intent.DestinationGroupIds.Select(g => g.Value).ToArray(),
                    intent.DestinationForm);
            })
        ];
    }

    private static List<StructureProgressionIntentDto> MapLegacyProgressionPaths(
        Stage stage,
        ProgressionRules rules)
    {
        var order = 1;

        // Group destinations share one intent. GroupId and MatchNumber stay out of the
        // coalesce key; destinationGroupIds follow Expand (BracketPair order).
        var groups = rules.Paths
            .GroupBy(path => (
                DestinationStageId: path.Destination.StageId.Value,
                path.Outcome,
                RoundId: ResolveProgressionRoundId(stage, path),
                path.Destination.TargetsPopulation,
                path.Destination.TargetsForm,
                path.Destination.TargetsGroup,
                GroupId: path.Destination.TargetsGroup ? null : path.Destination.GroupId?.Value))
            .OrderBy(g => g.Key.RoundId);

        var list = new List<StructureProgressionIntentDto>();
        foreach (var group in groups)
        {
            var paths = group.ToArray();
            var first = paths[0];
            var roundName = stage.Rounds.FirstOrDefault(r => r.Id.Value == group.Key.RoundId)?.Name;
            string[]? slotKeys = null;
            Guid[]? groupIds = null;
            if (group.Key is { TargetsPopulation: false, TargetsForm: false })
            {
                if (first.Destination.TargetsGroup)
                {
                    groupIds = DestinationGroupIdsInExpandOrder(stage, paths);
                }
                else
                {
                    slotKeys = [.. paths.Select(p => p.Destination.SlotKey!)];
                }
            }

            list.Add(new StructureProgressionIntentDto(
                LegacyIntentId(stage.Id.Value, "prog", order),
                order,
                group.Key.RoundId,
                roundName,
                group.Key.Outcome,
                group.Key.DestinationStageId,
                slotKeys,
                paths.Length,
                groupIds,
                group.Key.TargetsForm));
            order++;
        }

        return list;
    }

    private static Guid[] DestinationGroupIdsInExpandOrder(Stage stage, ProgressionPath[] paths)
    {
        var rank = new Dictionary<string, int>(StringComparer.Ordinal);
        var orderedPairs = stage.BracketPairs
            .OrderBy(pair => pair.PairKey, StringComparer.Ordinal)
            .ToArray();
        for (var i = 0; i < orderedPairs.Length; i++)
        {
            rank[orderedPairs[i].PairKey] = i;
        }

        return
        [
            .. paths
                .Select((path, index) => (path, index))
                .OrderBy(item => rank.TryGetValue(item.path.SourcePairKey, out var expandIndex)
                    ? expandIndex
                    : int.MaxValue)
                .ThenBy(item => item.path.SourcePairKey, StringComparer.Ordinal)
                .ThenBy(item => item.index)
                .Select(item => item.path.Destination.GroupId!.Value.Value)
        ];
    }

    private static Guid ResolveProgressionRoundId(Stage stage, ProgressionPath path)
    {
        if (stage.Rounds.Count == 1)
        {
            return stage.Rounds[0].Id.Value;
        }

        foreach (var round in stage.Rounds)
        {
            if (round.Fixtures.Any(f =>
                    string.Equals(f.BracketPairKey, path.SourcePairKey, StringComparison.Ordinal)))
            {
                return round.Id.Value;
            }
        }

        return stage.Rounds[0].Id.Value;
    }

    private static int CountIntentDestinations(QualificationIntent intent, int groupCount)
    {
        var span = intent.PositionTo - intent.PositionFrom + 1;
        return intent.SourceKind switch
        {
            QualificationIntentSourceKind.EachGroup => Math.Max(groupCount, 0) * span,
            QualificationIntentSourceKind.SingleGroup
                or QualificationIntentSourceKind.Overall
                or QualificationIntentSourceKind.AcrossGroups => span,
            _ => span
        };
    }

    private static Guid LegacyIntentId(Guid stageId, string kind, int order)
    {
        var hash = HashCode.Combine(stageId, kind, order);
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes, hash);
        BitConverter.TryWriteBytes(bytes.AsSpan(8), stageId.GetHashCode());
        return new Guid(bytes);
    }
}
