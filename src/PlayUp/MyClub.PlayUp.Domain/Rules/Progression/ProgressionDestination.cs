// -----------------------------------------------------------------------
// <copyright file="ProgressionDestination.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Where a progression outcome is routed.
/// Population: stage only.
/// Slot: stage + SlotKey (Cup form).
/// Group: stage + GroupId (Groups A1 Placement).
/// </summary>
public sealed record ProgressionDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim (aligned with qualification destinations).
    /// </summary>
    public const int SlotKeyMaxLength = Slot.SlotKeyMaxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionDestination"/> class.
    /// Exactly one of: population (both null), slot, or group.
    /// </summary>
    [JsonConstructor]
    public ProgressionDestination(StageId stageId, string? slotKey = null, GroupId? groupId = null)
    {
        if (slotKey is not null && groupId is not null)
        {
            throw new DomainException(
                "Progression destination cannot target both a slot and a group.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        StageId = stageId;
        SlotKey = slotKey is null ? null : NormalizeSlotKey(slotKey);
        GroupId = groupId;
    }

    /// <summary>Creates a population-targeting destination.</summary>
    public static ProgressionDestination ForPopulation(StageId stageId) =>
        new(stageId, slotKey: null, groupId: null);

    /// <summary>Creates a slot-targeting destination.</summary>
    public static ProgressionDestination ForSlot(StageId stageId, string slotKey) =>
        new(stageId, slotKey, groupId: null);

    /// <summary>Creates a group-targeting destination (Groups A1).</summary>
    public static ProgressionDestination ForGroup(StageId stageId, GroupId groupId) =>
        new(stageId, slotKey: null, groupId);

    /// <summary>Gets the destination stage identity.</summary>
    public StageId StageId { get; }

    /// <summary>Gets the opaque destination slot key when targeting Cup form; otherwise <see langword="null"/>.</summary>
    public string? SlotKey { get; }

    /// <summary>Gets the destination group when targeting Groups Placement; otherwise <see langword="null"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>Gets a value indicating whether this destination targets phase population only.</summary>
    public bool TargetsPopulation => SlotKey is null && GroupId is null;

    /// <summary>Gets a value indicating whether this destination targets a Cup slot.</summary>
    public bool TargetsSlot => SlotKey is not null;

    /// <summary>Gets a value indicating whether this destination targets a Groups poule.</summary>
    public bool TargetsGroup => GroupId is not null;

    /// <summary>Returns a copy of this destination.</summary>
    public ProgressionDestination Copy() =>
        TargetsPopulation
            ? ForPopulation(StageId)
            : TargetsGroup
                ? ForGroup(StageId, GroupId!.Value)
                : ForSlot(StageId, SlotKey!);

    private static string NormalizeSlotKey(string slotKey)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        var trimmed = slotKey.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException(
                "Slot key cannot be empty.",
                RulesErrorCodes.ProgressionRulesInvalid),
            > SlotKeyMaxLength => throw new DomainException(
                $"Slot key cannot exceed {SlotKeyMaxLength} characters.",
                RulesErrorCodes.ProgressionRulesInvalid),
            _ => trimmed
        };
    }
}
