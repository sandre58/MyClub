// -----------------------------------------------------------------------
// <copyright file="QualificationPathExpander.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Pure Expand: <see cref="QualificationIntent"/> → ordered <see cref="QualificationPath"/>
/// targeting destination stage population or Place (Auto).
/// Consumes <c>GroupOrder</c> from Structure — does not define it.
/// </summary>
public static class QualificationPathExpander
{
    /// <summary>
    /// Expands all intents into atomic paths (global order = intent order, then expansion order).
    /// </summary>
    /// <param name="intents">Authoring intents (unique orders).</param>
    /// <param name="groupOrder">Canonical group order of the source stage.</param>
    /// <returns>Ordered qualification paths for Apply.</returns>
    public static IReadOnlyList<QualificationPath> Materialize(
        IReadOnlyList<QualificationIntent> intents,
        IReadOnlyList<GroupId> groupOrder)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(groupOrder);

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

        foreach (var intent in intents.OrderBy(i => i.Order))
        {
            var occurrences = Expand(intent, groupOrder);
            if (occurrences.Count == 0)
            {
                throw new DomainException(
                    "Qualification intent produced no source occurrences.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            var condition = intent.Condition is null
                ? null
                : QualificationCondition.PointsAtLeast(intent.Condition.MinimumPoints);

            if (intent.TargetsPopulation)
            {
                var destination = QualificationDestination.ForPopulation(intent.DestinationStageId);
                paths.AddRange(occurrences.Select(occurrence => new QualificationPath(
                    pathOrder++,
                    ToSource(occurrence),
                    new QualificationSelection(SelectionMode.Position, occurrence.Position),
                    destination,
                    condition)));
                continue;
            }

            if (intent.TargetsForm)
            {
                // Expand zip: each Path i gets ForForm(DestinationStageId) — Form grain has no sub-id list.
                var destination = QualificationDestination.ForForm(intent.DestinationStageId);
                paths.AddRange(occurrences.Select(occurrence => new QualificationPath(
                    pathOrder++,
                    ToSource(occurrence),
                    new QualificationSelection(SelectionMode.Position, occurrence.Position),
                    destination,
                    condition)));
                continue;
            }

            if (intent.TargetsGroup)
            {
                if (intent.DestinationGroupIds.Count != occurrences.Count)
                {
                    throw new DomainException(
                        "Qualification place destination group ids count must equal Expand occurrence count.",
                        RulesErrorCodes.QualificationRulesInvalid);
                }

                for (var i = 0; i < occurrences.Count; i++)
                {
                    var occurrence = occurrences[i];
                    var destination = QualificationDestination.ForGroup(
                        intent.DestinationStageId,
                        intent.DestinationGroupIds[i]);
                    paths.Add(new QualificationPath(
                        pathOrder++,
                        ToSource(occurrence),
                        new QualificationSelection(SelectionMode.Position, occurrence.Position),
                        destination,
                        condition));
                }

                continue;
            }

            if (intent.DestinationSlotKeys.Count != occurrences.Count)
            {
                throw new DomainException(
                    "Qualification place destination slot keys count must equal Expand occurrence count.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            for (var i = 0; i < occurrences.Count; i++)
            {
                var occurrence = occurrences[i];
                var destination = QualificationDestination.ForSlot(
                    intent.DestinationStageId,
                    intent.DestinationSlotKeys[i]);
                paths.Add(new QualificationPath(
                    pathOrder++,
                    ToSource(occurrence),
                    new QualificationSelection(SelectionMode.Position, occurrence.Position),
                    destination,
                    condition));
            }
        }

        return paths;
    }

    /// <summary>
    /// Expands one intent into ordered source occurrences.
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
    /// Migrates a legacy atomic path into a singleton intent.
    /// </summary>
    public static QualificationIntent ToSingletonIntent(QualificationPath path, IntentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Selection.Mode != SelectionMode.Position)
        {
            throw new DomainException(
                "Legacy non-Position paths cannot migrate to QualificationIntent without Position selection.",
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

        IReadOnlyList<string>? slotKeys = path.Destination.TargetsSlot
            ? [path.Destination.SlotKey!]
            : null;
        IReadOnlyList<GroupId>? destGroupIds = path.Destination.TargetsGroup
            ? [path.Destination.GroupId!.Value]
            : null;

        return new QualificationIntent(
            id ?? IntentId.New(),
            path.Order,
            kind,
            k,
            k,
            path.Destination.StageId,
            groupId,
            across,
            path.Condition,
            slotKeys,
            destGroupIds,
            destinationForm: path.Destination.TargetsForm);
    }

    private static QualificationSource ToSource(QualificationSourceOccurrence occurrence) =>
        occurrence.Scope switch
        {
            RankingScope.Group => QualificationSource.FromGroup(occurrence.GroupId!.Value),
            RankingScope.AcrossGroups => QualificationSource.AcrossGroups(occurrence.AcrossGroupsPosition!.Value),
            _ => QualificationSource.Overall()
        };
}
