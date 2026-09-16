// -----------------------------------------------------------------------
// <copyright file="QualificationPathExpander.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Pure Expand + Map: <see cref="QualificationIntent"/> → ordered <see cref="QualificationPath"/>.
/// Consumes <c>GroupOrder</c> / <c>SlotOrder</c> from Structure — does not define them.
/// </summary>
public static class QualificationPathExpander
{
    /// <summary>
    /// Expands all intents into atomic paths (global order = intent order, then expansion order).
    /// </summary>
    /// <param name="intents">Authoring intents (unique orders).</param>
    /// <param name="groupOrder">Canonical group order of the source stage.</param>
    /// <param name="slotOrderByStage">Canonical slot keys per destination stage.</param>
    /// <returns>Ordered qualification paths for Apply / WhoFeeds.</returns>
    public static IReadOnlyList<QualificationPath> Materialize(
        IReadOnlyList<QualificationIntent> intents,
        IReadOnlyList<GroupId> groupOrder,
        IReadOnlyDictionary<StageId, IReadOnlyList<string>> slotOrderByStage)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(groupOrder);
        ArgumentNullException.ThrowIfNull(slotOrderByStage);

        if (intents.Count == 0)
        {
            throw new DomainException(
                "Qualification rules require at least one intent.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (intents.Select(i => i.Order).Distinct().Count() != intents.Count)
        {
            throw new DomainException(
                "Qualification intent orders must be unique.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (intents.Select(i => i.Id).Distinct().Count() != intents.Count)
        {
            throw new DomainException(
                "Qualification intent ids must be unique.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        var paths = new List<QualificationPath>();
        var pathOrder = 1;
        var usedSlots = new HashSet<(Guid Stage, string Slot)>();

        foreach (var intent in intents.OrderBy(i => i.Order))
        {
            var occurrences = Expand(intent, groupOrder);
            if (occurrences.Count == 0)
            {
                throw new DomainException(
                    "Qualification intent produced no source occurrences.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            if (!slotOrderByStage.TryGetValue(intent.DestinationStageId, out var slotOrder)
                || slotOrder.Count == 0)
            {
                throw new DomainException(
                    "Destination stage has no slots for qualification mapping.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            if (occurrences.Count > slotOrder.Count)
            {
                throw new DomainException(
                    $"Qualification intent requires {occurrences.Count} destinations but only {slotOrder.Count} slots are available.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            var mapped = Map(intent, occurrences, slotOrder);
            foreach (var (occurrence, slotKey) in mapped)
            {
                var key = (intent.DestinationStageId.Value, slotKey);
                if (!usedSlots.Add(key))
                {
                    throw new DomainException(
                        $"Destination slot '{slotKey}' is fed by more than one qualification path.",
                        RulesErrorCodes.QualificationRulesInvalid);
                }

                paths.Add(
                    new QualificationPath(
                        pathOrder++,
                        ToSource(occurrence),
                        new QualificationSelection(SelectionMode.Position, occurrence.Position),
                        new QualificationDestination(intent.DestinationStageId, slotKey),
                        intent.Condition is null ? null : QualificationCondition.PointsAtLeast(intent.Condition.MinimumPoints)));
            }
        }

        return paths;
    }

    /// <summary>
    /// Expands one intent into ordered source occurrences (no destinations yet).
    /// </summary>
    public static IReadOnlyList<QualificationSourceOccurrence> Expand(
        QualificationIntent intent,
        IReadOnlyList<GroupId> groupOrder)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(groupOrder);

        var list = new List<QualificationSourceOccurrence>();
        switch (intent.SourceKind)
        {
            case QualificationIntentSourceKind.EachGroup:
                if (groupOrder.Count == 0)
                {
                    throw new DomainException(
                        "Each-group intent requires at least one group in GroupOrder.",
                        RulesErrorCodes.QualificationRulesInvalid);
                }

                foreach (var groupId in groupOrder)
                {
                    for (var k = intent.PositionFrom; k <= intent.PositionTo; k++)
                    {
                        list.Add(QualificationSourceOccurrence.Group(groupId, k));
                    }
                }

                break;

            case QualificationIntentSourceKind.SingleGroup:
                for (var k = intent.PositionFrom; k <= intent.PositionTo; k++)
                {
                    list.Add(QualificationSourceOccurrence.Group(intent.GroupId!.Value, k));
                }

                break;

            case QualificationIntentSourceKind.Overall:
                for (var k = intent.PositionFrom; k <= intent.PositionTo; k++)
                {
                    list.Add(QualificationSourceOccurrence.Overall(k));
                }

                break;

            case QualificationIntentSourceKind.AcrossGroups:
                var p = intent.AcrossGroupsPosition!.Value;
                for (var k = intent.PositionFrom; k <= intent.PositionTo; k++)
                {
                    list.Add(QualificationSourceOccurrence.AcrossGroups(p, k));
                }

                break;

            default:
                throw new DomainException(
                    "Qualification intent source kind is unknown.",
                    RulesErrorCodes.QualificationRulesInvalid);
        }

        return list;
    }

    /// <summary>
    /// Maps occurrences to slot keys (canonical zip, then compatible Custom overrides).
    /// Orphan overrides (occurrence no longer in Expand) are ignored here — caller must treat them explicitly in UX.
    /// </summary>
    public static IReadOnlyList<(QualificationSourceOccurrence Occurrence, string SlotKey)> Map(
        QualificationIntent intent,
        IReadOnlyList<QualificationSourceOccurrence> occurrences,
        IReadOnlyList<string> slotOrder)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(occurrences);
        ArgumentNullException.ThrowIfNull(slotOrder);

        if (occurrences.Count > slotOrder.Count)
        {
            throw new DomainException(
                $"Qualification intent requires {occurrences.Count} destinations but only {slotOrder.Count} slots are available.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        var result = new List<(QualificationSourceOccurrence, string)>(occurrences.Count);
        for (var i = 0; i < occurrences.Count; i++)
        {
            var occurrence = occurrences[i];
            var slotKey = slotOrder[i];
            if (intent.MappingMode == QualificationMappingMode.Custom)
            {
                var match = intent.SlotOverrides.FirstOrDefault(o => o.Occurrence.Equals(occurrence));
                if (match is not null)
                {
                    slotKey = match.SlotKey;
                }
            }

            result.Add((occurrence, slotKey));
        }

        var distinct = result.Select(r => r.Item2).Distinct(StringComparer.Ordinal).Count();
        return distinct != result.Count
            ? throw new DomainException(
                "Two source occurrences map to the same destination slot.",
                RulesErrorCodes.QualificationRulesInvalid)
            : (IReadOnlyList<(QualificationSourceOccurrence Occurrence, string SlotKey)>)result;
    }

    /// <summary>
    /// Migrates a legacy atomic path into a singleton intent (Canonical).
    /// </summary>
    public static QualificationIntent ToSingletonIntent(QualificationPath path, IntentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Selection.Mode != SelectionMode.Position)
        {
            // Legacy Top/Best/etc. kept as single-position intent only when Position;
            // non-Position paths become Overall/Group singleton with From=To=Value as Position intent
            // is the V1 authoring contract — preserve via Position(value) when possible.
            throw new DomainException(
                "Legacy non-Position paths cannot migrate to QualificationIntent V1 without Position selection.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        var k = path.Selection.Value;
        var scope = path.Source.Scope;
        QualificationIntentSourceKind kind;
        GroupId? groupId = null;
        int? across = null;

        if (scope == RankingScope.AcrossGroups || path.Source.AcrossGroupsPosition is not null)
        {
            kind = QualificationIntentSourceKind.AcrossGroups;
            across = path.Source.AcrossGroupsPosition
                ?? throw new DomainException(
                    "Across-groups path requires AcrossGroupsPosition.",
                    RulesErrorCodes.QualificationRulesInvalid);
        }
        else if (scope == RankingScope.Group || path.Source.GroupId is not null)
        {
            kind = QualificationIntentSourceKind.SingleGroup;
            groupId = path.Source.GroupId
                ?? throw new DomainException(
                    "Group path requires GroupId.",
                    RulesErrorCodes.QualificationRulesInvalid);
        }
        else
        {
            kind = QualificationIntentSourceKind.Overall;
        }

        return new QualificationIntent(
            id ?? IntentId.New(),
            path.Order,
            kind,
            k,
            k,
            path.Destination.StageId,
            QualificationMappingMode.Custom,
            groupId,
            across,
            path.Condition,
            [new QualificationSlotOverride(ToOccurrence(path), path.Destination.SlotKey)]);
    }

    private static QualificationSourceOccurrence ToOccurrence(QualificationPath path)
    {
        var k = path.Selection.Value;
        return path.Source.AcrossGroupsPosition is { } p
            ? QualificationSourceOccurrence.AcrossGroups(p, k)
            : path.Source.GroupId is { } g ? QualificationSourceOccurrence.Group(g, k) : QualificationSourceOccurrence.Overall(k);
    }

    private static QualificationSource ToSource(QualificationSourceOccurrence occurrence) =>
        occurrence.Scope switch
        {
            RankingScope.Group => QualificationSource.FromGroup(occurrence.GroupId!.Value),
            RankingScope.AcrossGroups => QualificationSource.AcrossGroups(occurrence.AcrossGroupsPosition!.Value),
            _ => QualificationSource.Overall()
        };
}
