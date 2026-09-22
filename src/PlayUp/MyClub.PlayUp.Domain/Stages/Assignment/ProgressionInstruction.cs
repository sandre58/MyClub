// -----------------------------------------------------------------------
// <copyright file="ProgressionInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of applying a progression path: population and optional Placement grain (slot or group).
/// Not persisted — Application mutates <see cref="Stage"/> accordingly.
/// </summary>
/// <param name="StageId">Destination stage.</param>
/// <param name="EntryId">Resolved competition entry.</param>
/// <param name="SlotKey">Cup slot when targeting form; <see langword="null"/> otherwise.</param>
/// <param name="GroupId">Groups poule when targeting Groups Placement; <see langword="null"/> otherwise.</param>
public sealed record ProgressionInstruction(
    StageId StageId,
    EntryId EntryId,
    string? SlotKey = null,
    GroupId? GroupId = null)
{
    /// <summary>Gets a value indicating whether this instruction targets phase population only.</summary>
    public bool TargetsPopulation => SlotKey is null && GroupId is null;

    /// <summary>Gets a value indicating whether this instruction targets a Cup slot.</summary>
    public bool TargetsSlot => SlotKey is not null;

    /// <summary>Gets a value indicating whether this instruction targets a Groups poule.</summary>
    public bool TargetsGroup => GroupId is not null;
}
