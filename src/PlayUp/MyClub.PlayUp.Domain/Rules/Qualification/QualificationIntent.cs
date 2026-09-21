// -----------------------------------------------------------------------
// <copyright file="QualificationIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Persisted authoring unit: one selection intention that expands to N <see cref="QualificationPath"/>.
/// Paths are derived (Expand) — Intent is the authoring source of truth.
/// Destination is peer Population or Place (Auto) on the destination stage form.
/// </summary>
/// <remarks>
/// Place is a total function Expand → Place: <see cref="DestinationSlotKeys"/> count must equal
/// Expand occurrence count, with index alignment. Empty/null keys = Population (same destination
/// applied to every path). Partial Place mapping is invalid.
/// </remarks>
public sealed record QualificationIntent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationIntent"/> class.
    /// </summary>
    public QualificationIntent(
        IntentId id,
        int order,
        QualificationIntentSourceKind sourceKind,
        int positionFrom,
        int positionTo,
        StageId destinationStageId,
        GroupId? groupId = null,
        int? acrossGroupsPosition = null,
        QualificationCondition? condition = null,
        IReadOnlyList<string>? destinationSlotKeys = null)
    {
        if (!Enum.IsDefined(sourceKind))
        {
            throw new DomainException(
                "Qualification intent source kind is unknown.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (order < 1)
        {
            throw new DomainException(
                "Qualification intent order must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (positionFrom < 1)
        {
            throw new DomainException(
                "Position from must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (positionTo < positionFrom)
        {
            throw new DomainException(
                "Position to must be greater than or equal to position from.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        switch (sourceKind)
        {
            case QualificationIntentSourceKind.SingleGroup when groupId is null:
                throw new DomainException(
                    "Single-group intent requires a group identity.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case QualificationIntentSourceKind.SingleGroup when acrossGroupsPosition is not null:
                throw new DomainException(
                    "Single-group intent cannot carry an across-groups position.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case QualificationIntentSourceKind.EachGroup when groupId is not null || acrossGroupsPosition is not null:
                throw new DomainException(
                    "Each-group intent cannot carry group id or across-groups position.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case QualificationIntentSourceKind.Overall when groupId is not null || acrossGroupsPosition is not null:
                throw new DomainException(
                    "Overall intent cannot carry group id or across-groups position.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case QualificationIntentSourceKind.AcrossGroups when acrossGroupsPosition is null or < 1:
                throw new DomainException(
                    "Across-groups intent requires position P ≥ 1.",
                    RulesErrorCodes.QualificationRulesInvalid);
            case QualificationIntentSourceKind.AcrossGroups when groupId is not null:
                throw new DomainException(
                    "Across-groups intent cannot target a specific group.",
                    RulesErrorCodes.QualificationRulesInvalid);
            default:
                break;
        }

        Id = id;
        Order = order;
        SourceKind = sourceKind;
        PositionFrom = positionFrom;
        PositionTo = positionTo;
        DestinationStageId = destinationStageId;
        GroupId = groupId;
        AcrossGroupsPosition = acrossGroupsPosition;
        Condition = condition;
        DestinationSlotKeys = NormalizeDestinationSlotKeys(destinationStageId, destinationSlotKeys);
    }

    /// <summary>Gets the stable authoring identity (Guid v7).</summary>
    public IntentId Id { get; }

    /// <summary>Gets the display / processing order among intents (≥ 1).</summary>
    public int Order { get; }

    /// <summary>Gets the authoring source kind.</summary>
    public QualificationIntentSourceKind SourceKind { get; }

    /// <summary>Gets the inclusive lower bound of selection positions.</summary>
    public int PositionFrom { get; }

    /// <summary>Gets the inclusive upper bound of selection positions.</summary>
    public int PositionTo { get; }

    /// <summary>Gets the destination stage.</summary>
    public StageId DestinationStageId { get; }

    /// <summary>
    /// Gets Place destination slot keys (Expand index ↔ key index). Empty = Population.
    /// </summary>
    public IReadOnlyList<string> DestinationSlotKeys { get; }

    /// <summary>Gets the group when <see cref="SourceKind"/> is <see cref="QualificationIntentSourceKind.SingleGroup"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>Gets P when <see cref="SourceKind"/> is <see cref="QualificationIntentSourceKind.AcrossGroups"/>.</summary>
    public int? AcrossGroupsPosition { get; }

    /// <summary>Gets the optional Points gate applied to each generated Position path.</summary>
    public QualificationCondition? Condition { get; }

    /// <summary>Gets a value indicating whether this intent targets population only.</summary>
    public bool TargetsPopulation => DestinationSlotKeys.Count == 0;

    /// <summary>Returns a deep copy.</summary>
    public QualificationIntent Copy() =>
        new(
            Id,
            Order,
            SourceKind,
            PositionFrom,
            PositionTo,
            DestinationStageId,
            GroupId,
            AcrossGroupsPosition,
            Condition is null ? null : QualificationCondition.PointsAtLeast(Condition.MinimumPoints),
            DestinationSlotKeys);

    private static string[] NormalizeDestinationSlotKeys(
        StageId destinationStageId,
        IReadOnlyList<string>? destinationSlotKeys)
    {
        if (destinationSlotKeys is null || destinationSlotKeys.Count == 0)
        {
            return [];
        }

        var normalized = new string[destinationSlotKeys.Count];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < destinationSlotKeys.Count; i++)
        {
            var key = destinationSlotKeys[i];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new DomainException(
                    "Qualification place destination slot keys cannot be empty.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            var slotKey = QualificationDestination.ForSlot(destinationStageId, key).SlotKey!;
            if (!seen.Add(slotKey))
            {
                throw new DomainException(
                    "Qualification place destination slot keys must be unique within an intent.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            normalized[i] = slotKey;
        }

        return normalized;
    }
}
