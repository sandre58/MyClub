// -----------------------------------------------------------------------
// <copyright file="QualificationDestination.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Where selected participants are routed.
/// Population: stage only.
/// Slot: stage + SlotKey (Cup Auto Place — dual-write with population).
/// Group: stage + GroupId (Groups A1 Placement — dual-write with population).
/// </summary>
public sealed record QualificationDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim (aligned with progression destinations).
    /// </summary>
    public const int SlotKeyMaxLength = Slot.SlotKeyMaxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationDestination"/> class.
    /// Exactly one of: population (both null), slot, or group.
    /// </summary>
    /// <param name="stageId">The destination stage identity.</param>
    /// <param name="slotKey">Slot key when targeting Cup form; otherwise <see langword="null"/>.</param>
    /// <param name="groupId">Group when targeting Groups Placement; otherwise <see langword="null"/>.</param>
    [JsonConstructor]
    public QualificationDestination(StageId stageId, string? slotKey = null, GroupId? groupId = null)
    {
        if (slotKey is not null && groupId is not null)
        {
            throw new DomainException(
                "Qualification destination cannot target both a slot and a group.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        StageId = stageId;
        SlotKey = slotKey is null ? null : NormalizeSlotKey(slotKey);
        GroupId = groupId;
    }

    /// <summary>
    /// Creates a population-targeting destination (StageId only).
    /// </summary>
    public static QualificationDestination ForPopulation(StageId stageId) =>
        new(stageId, slotKey: null, groupId: null);

    /// <summary>
    /// Creates a slot-targeting destination (Auto Place into Cup form).
    /// </summary>
    public static QualificationDestination ForSlot(StageId stageId, string slotKey) =>
        new(stageId, slotKey, groupId: null);

    /// <summary>
    /// Creates a group-targeting destination (Groups A1 Placement).
    /// </summary>
    public static QualificationDestination ForGroup(StageId stageId, GroupId groupId) =>
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
    public QualificationDestination Copy() =>
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
                RulesErrorCodes.QualificationRulesInvalid),
            > SlotKeyMaxLength => throw new DomainException(
                $"Slot key cannot exceed {SlotKeyMaxLength} characters.",
                RulesErrorCodes.QualificationRulesInvalid),
            _ => trimmed
        };
    }
}
