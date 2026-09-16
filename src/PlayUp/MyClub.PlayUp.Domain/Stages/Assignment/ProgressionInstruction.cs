// -----------------------------------------------------------------------
// <copyright file="ProgressionInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of applying a progression path: either add to phase population or place into a slot.
/// Not persisted — Application mutates <see cref="Stage"/> accordingly.
/// </summary>
/// <param name="StageId">Destination stage.</param>
/// <param name="EntryId">Resolved competition entry.</param>
/// <param name="SlotKey">Slot when targeting form; <see langword="null"/> when targeting population.</param>
public sealed record ProgressionInstruction(StageId StageId, EntryId EntryId, string? SlotKey)
{
    /// <summary>
    /// Gets a value indicating whether this instruction targets phase population.
    /// </summary>
    public bool TargetsPopulation => SlotKey is null;
}
