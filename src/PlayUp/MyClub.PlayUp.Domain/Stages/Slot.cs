// -----------------------------------------------------------------------
// <copyright file="Slot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Positional placeholder within a stage (bracket / draw destination).
/// Business identity is <see cref="SlotKey"/>; <see cref="EntryId"/> is resolution only (never a feed).
/// </summary>
[DebuggerDisplay("{SlotKey} → {EntryId}")]
public sealed class Slot : Entity<string>
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim.
    /// </summary>
    public const int SlotKeyMaxLength = 100;

    internal Slot(string slotKey)
        : base(NormalizeKey(slotKey))
    {
    }

    /// <summary>
    /// Gets the business slot key (same as <see cref="Entity{TId}.Id"/>).
    /// </summary>
    public string SlotKey => Id;

    /// <summary>
    /// Gets the currently resolved occupant, if any.
    /// </summary>
    public EntryId? EntryId { get; private set; }

    internal static string NormalizeKey(string slotKey)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        var trimmed = slotKey.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException(
                "Slot key cannot be empty.",
                StageErrorCodes.SlotKeyInvalid),
            > SlotKeyMaxLength => throw new DomainException(
                $"Slot key cannot exceed {SlotKeyMaxLength} characters.",
                StageErrorCodes.SlotKeyInvalid),
            _ => trimmed
        };
    }

    internal void SetEntry(EntryId entryId) => EntryId = entryId;

    internal void ClearEntry() => EntryId = null;
}
