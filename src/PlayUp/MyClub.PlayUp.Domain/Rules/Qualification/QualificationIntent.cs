// -----------------------------------------------------------------------
// <copyright file="QualificationIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Persisted authoring unit: one selection intention that expands to N <see cref="QualificationPath"/>.
/// Paths are derived (Expand + Map) — Intent is the authoring source of truth.
/// </summary>
public sealed record QualificationIntent
{
    private readonly QualificationSlotOverride[] _slotOverrides;

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
        QualificationMappingMode mappingMode = QualificationMappingMode.Canonical,
        GroupId? groupId = null,
        int? acrossGroupsPosition = null,
        QualificationCondition? condition = null,
        IReadOnlyList<QualificationSlotOverride>? slotOverrides = null)
    {
        if (!Enum.IsDefined(sourceKind))
        {
            throw new DomainException(
                "Qualification intent source kind is unknown.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (!Enum.IsDefined(mappingMode))
        {
            throw new DomainException(
                "Qualification mapping mode is unknown.",
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

        var overrides = slotOverrides?.ToArray() ?? [];
        if (mappingMode == QualificationMappingMode.Canonical && overrides.Length > 0)
        {
            throw new DomainException(
                "Canonical mapping cannot carry slot overrides.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (overrides.Select(o => o.Occurrence).Distinct().Count() != overrides.Length)
        {
            throw new DomainException(
                "Slot overrides must have unique source occurrences.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        foreach (var item in overrides)
        {
            ArgumentNullException.ThrowIfNull(item.Occurrence);
            _ = new QualificationDestination(destinationStageId, item.SlotKey);
        }

        Id = id;
        Order = order;
        SourceKind = sourceKind;
        PositionFrom = positionFrom;
        PositionTo = positionTo;
        DestinationStageId = destinationStageId;
        MappingMode = mappingMode;
        GroupId = groupId;
        AcrossGroupsPosition = acrossGroupsPosition;
        Condition = condition;
        _slotOverrides = overrides;
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

    /// <summary>Gets the single destination stage (V1).</summary>
    public StageId DestinationStageId { get; }

    /// <summary>Gets the mapping mode.</summary>
    public QualificationMappingMode MappingMode { get; }

    /// <summary>Gets the group when <see cref="SourceKind"/> is <see cref="QualificationIntentSourceKind.SingleGroup"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>Gets P when <see cref="SourceKind"/> is <see cref="QualificationIntentSourceKind.AcrossGroups"/>.</summary>
    public int? AcrossGroupsPosition { get; }

    /// <summary>Gets the optional Points gate applied to each generated Position path.</summary>
    public QualificationCondition? Condition { get; }

    /// <summary>Gets slot overrides (Custom only).</summary>
    public IReadOnlyList<QualificationSlotOverride> SlotOverrides => _slotOverrides;

    /// <summary>Returns a deep copy.</summary>
    public QualificationIntent Copy() =>
        new(
            Id,
            Order,
            SourceKind,
            PositionFrom,
            PositionTo,
            DestinationStageId,
            MappingMode,
            GroupId,
            AcrossGroupsPosition,
            Condition is null ? null : QualificationCondition.PointsAtLeast(Condition.MinimumPoints),
            _slotOverrides.Length == 0 ? null : [.. _slotOverrides.Select(o => new QualificationSlotOverride(o.Occurrence, o.SlotKey))]);
}
