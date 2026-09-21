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
/// Slot target: destination stage + opaque slot key (Auto Place — dual-write with population).
/// Population target: destination stage only.
/// </summary>
public sealed record QualificationDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim (aligned with progression destinations).
    /// </summary>
    public const int SlotKeyMaxLength = Slot.SlotKeyMaxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationDestination"/> class.
    /// Pass a non-empty <paramref name="slotKey"/> for form placement;
    /// pass <see langword="null"/> for population target.
    /// </summary>
    /// <param name="stageId">The destination stage identity.</param>
    /// <param name="slotKey">Slot key when targeting form; <see langword="null"/> when targeting population.</param>
    [JsonConstructor]
    public QualificationDestination(StageId stageId, string? slotKey)
    {
        StageId = stageId;
        SlotKey = slotKey is null ? null : NormalizeSlotKey(slotKey);
    }

    /// <summary>
    /// Creates a population-targeting destination (StageId only).
    /// </summary>
    /// <param name="stageId">Destination stage whose population receives the entry.</param>
    /// <returns>A population destination.</returns>
    public static QualificationDestination ForPopulation(StageId stageId) => new(stageId, slotKey: null);

    /// <summary>
    /// Creates a slot-targeting destination (Auto Place into form).
    /// </summary>
    /// <param name="stageId">Destination stage.</param>
    /// <param name="slotKey">Destination slot key.</param>
    /// <returns>A slot destination.</returns>
    public static QualificationDestination ForSlot(StageId stageId, string slotKey) => new(stageId, slotKey);

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the opaque destination slot key when targeting form; otherwise <see langword="null"/>.
    /// </summary>
    public string? SlotKey { get; }

    /// <summary>
    /// Gets a value indicating whether this destination targets the phase population (no slot).
    /// </summary>
    public bool TargetsPopulation => SlotKey is null;

    /// <summary>Returns a copy of this destination.</summary>
    public QualificationDestination Copy() =>
        TargetsPopulation ? ForPopulation(StageId) : ForSlot(StageId, SlotKey!);

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
