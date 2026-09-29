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
/// Placement / population destination (one-of).
/// Population | Form (Championship/Swiss) | Group (Groups) | Slot (Cup).
/// </summary>
public sealed record ProgressionDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim (aligned with qualification destinations).
    /// </summary>
    public const int SlotKeyMaxLength = Slot.SlotKeyMaxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionDestination"/> class.
    /// One-of: population (defaults), form, slot, or group.
    /// </summary>
    [JsonConstructor]
    public ProgressionDestination(
        StageId stageId,
        string? slotKey = null,
        GroupId? groupId = null,
        bool form = false)
    {
        var hasSlot = slotKey is not null;
        var hasGroup = groupId is not null;
        var modes = (form ? 1 : 0) + (hasSlot ? 1 : 0) + (hasGroup ? 1 : 0);
        if (modes > 1)
        {
            throw new DomainException(
                "Progression destination must be exactly one of: population, form, slot, or group.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        StageId = stageId;
        SlotKey = hasSlot ? NormalizeSlotKey(slotKey!) : null;
        GroupId = groupId;
        Form = form;
    }

    /// <summary>Creates a population-only destination (no form feed).</summary>
    public static ProgressionDestination ForPopulation(StageId stageId) =>
        new(stageId, slotKey: null, groupId: null, form: false);

    /// <summary>Creates a form Placement destination (Championship / Swiss).</summary>
    public static ProgressionDestination ForForm(StageId stageId) =>
        new(stageId, slotKey: null, groupId: null, form: true);

    /// <summary>Creates a slot Placement destination.</summary>
    public static ProgressionDestination ForSlot(StageId stageId, string slotKey) =>
        new(stageId, slotKey, groupId: null, form: false);

    /// <summary>Creates a group Placement destination (Groups).</summary>
    public static ProgressionDestination ForGroup(StageId stageId, GroupId groupId) =>
        new(stageId, slotKey: null, groupId, form: false);

    /// <summary>Gets the destination stage identity.</summary>
    public StageId StageId { get; }

    /// <summary>Gets the Cup slot key when <see cref="TargetsSlot"/>; otherwise <see langword="null"/>.</summary>
    public string? SlotKey { get; }

    /// <summary>Gets the Groups poule when <see cref="TargetsGroup"/>; otherwise <see langword="null"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>
    /// Gets a value indicating whether this destination is Form Placement (persisted discriminator vs Population).
    /// </summary>
    public bool Form { get; }

    /// <summary>Gets a value indicating whether this destination is population only.</summary>
    public bool TargetsPopulation => !Form && SlotKey is null && GroupId is null;

    /// <summary>Gets a value indicating whether this destination is Form Placement.</summary>
    public bool TargetsForm => Form;

    /// <summary>Gets a value indicating whether this destination targets a Cup slot.</summary>
    public bool TargetsSlot => SlotKey is not null;

    /// <summary>Gets a value indicating whether this destination targets a Groups poule.</summary>
    public bool TargetsGroup => GroupId is not null;

    /// <summary>Returns a copy of this destination.</summary>
    public ProgressionDestination Copy() =>
        TargetsPopulation
            ? ForPopulation(StageId)
            : TargetsForm
                ? ForForm(StageId)
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
