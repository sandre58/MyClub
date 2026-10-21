// -----------------------------------------------------------------------
// <copyright file="ProgressionInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of applying a progression path: population and optional Placement grain.
/// Mirrors <see cref="ProgressionDestination"/> one-of — not persisted.
/// Application Apply still consumes <see cref="ProgressionPath.Destination"/> as SoT for writes.
/// </summary>
public sealed record ProgressionInstruction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionInstruction"/> class.
    /// One-of: population | form | slot | group.
    /// </summary>
    public ProgressionInstruction(
        StageId stageId,
        EntryId entryId,
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
                "Progression instruction must be exactly one of: population, form, slot, or group.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        StageId = stageId;
        EntryId = entryId;
        SlotKey = slotKey;
        GroupId = groupId;
        Form = form;
    }

    /// <summary>Gets the destination stage.</summary>
    public StageId StageId { get; }

    /// <summary>Gets the resolved competition entry.</summary>
    public EntryId EntryId { get; }

    /// <summary>Gets the Cup slot when <see cref="TargetsSlot"/>; otherwise <see langword="null"/>.</summary>
    public string? SlotKey { get; }

    /// <summary>Gets the Groups poule when <see cref="TargetsGroup"/>; otherwise <see langword="null"/>.</summary>
    public GroupId? GroupId { get; }

    /// <summary>Gets a value indicating whether this instruction is Form Placement.</summary>
    public bool Form { get; }

    /// <summary>Gets a value indicating whether this instruction targets phase population only.</summary>
    public bool TargetsPopulation => !Form && SlotKey is null && GroupId is null;

    /// <summary>Gets a value indicating whether this instruction targets Form Placement.</summary>
    public bool TargetsForm => Form;

    /// <summary>Gets a value indicating whether this instruction targets a Cup slot.</summary>
    public bool TargetsSlot => SlotKey is not null;

    /// <summary>Gets a value indicating whether this instruction targets a Groups poule.</summary>
    public bool TargetsGroup => GroupId is not null;
}
