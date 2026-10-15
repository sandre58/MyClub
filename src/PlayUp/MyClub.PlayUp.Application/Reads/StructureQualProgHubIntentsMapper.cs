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
        QualificationRules? rules)
    {
        if (rules is null || rules.Paths.Count == 0)
        {
            return null;
        }

        if (rules.Intents.Count > 0)
        {
            return
            [
                .. rules.Intents.Select(intent => MapQualificationIntent(stage, intent))
            ];
        }

        return
        [
            .. rules.Paths.Select(path => MapLegacyQualificationPath(stage, path))
        ];
    }

    internal static IReadOnlyList<StructureProgressionIntentDto>? MapProgressionIntents(
        Stage stage,
        ProgressionRules? rules)
    {
        if (rules is null || rules.Paths.Count == 0)
        {
            return null;
        }

        if (rules.Intents.Count > 0)
        {
            return MapPersistedProgressionIntents(stage, rules);
        }

        return MapLegacyProgressionPaths(stage, rules);
    }

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

    private static IReadOnlyList<StructureProgressionIntentDto> MapLegacyProgressionPaths(
        Stage stage,
        ProgressionRules rules)
    {
        var order = 1;
        var groups = rules.Paths
            .GroupBy(path => (
                DestinationStageId: path.Destination.StageId.Value,
                path.Outcome,
                RoundId: ResolveProgressionRoundId(stage, path),
                path.Destination.TargetsPopulation,
                path.Destination.TargetsForm,
                GroupId: path.Destination.GroupId?.Value))
            .OrderBy(g => g.Key.RoundId);

        var list = new List<StructureProgressionIntentDto>();
        foreach (var group in groups)
        {
            var paths = group.ToArray();
            var first = paths[0];
            var roundName = stage.Rounds.FirstOrDefault(r => r.Id.Value == group.Key.RoundId)?.Name;
            string[]? slotKeys = null;
            Guid[]? groupIds = null;
            if (!group.Key.TargetsPopulation && !group.Key.TargetsForm)
            {
                if (first.Destination.TargetsGroup)
                {
                    groupIds = paths.Select(p => p.Destination.GroupId!.Value.Value).ToArray();
                }
                else
                {
                    slotKeys = paths.Select(p => p.Destination.SlotKey!).ToArray();
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
