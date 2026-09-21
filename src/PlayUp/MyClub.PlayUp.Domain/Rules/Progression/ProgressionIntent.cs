// -----------------------------------------------------------------------
// <copyright file="ProgressionIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Persisted authoring unit: one round × outcome intention that expands to N <see cref="ProgressionPath"/>.
/// Paths are derived (Expand) — Intent is the authoring source of truth (Qual/Prog V3).
/// Destination is peer Population or Place on the destination stage form.
/// </summary>
/// <remarks>
/// Flattened like <see cref="QualificationIntent"/>: one destination stage + optional Place slot keys.
/// Place is a total function Expand → Place: <see cref="DestinationSlotKeys"/> count must equal
/// fixture count, with index alignment (fixture storage order ↔ key index). Empty/null = Population
/// (same destination stage applied to every path). Partial Place mapping is invalid.
/// Path-level <see cref="ProgressionDestination"/> remains single-slot (ForSlot / ForPopulation).
/// </remarks>
public sealed record ProgressionIntent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionIntent"/> class.
    /// </summary>
    public ProgressionIntent(
        IntentId id,
        int order,
        RoundId roundId,
        ProgressionOutcome outcome,
        StageId destinationStageId,
        IReadOnlyList<string>? destinationSlotKeys = null)
    {
        if (order < 1)
        {
            throw new DomainException(
                "Progression intent order must be at least 1.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new DomainException(
                "Progression intent outcome is unknown.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        Id = id;
        Order = order;
        RoundId = roundId;
        Outcome = outcome;
        DestinationStageId = destinationStageId;
        DestinationSlotKeys = NormalizeDestinationSlotKeys(destinationStageId, destinationSlotKeys);
    }

    /// <summary>Gets the stable authoring identity (Guid v7).</summary>
    public IntentId Id { get; }

    /// <summary>Gets the display / processing order among intents (≥ 1).</summary>
    public int Order { get; }

    /// <summary>Gets the source round whose fixtures expand to paths.</summary>
    public RoundId RoundId { get; }

    /// <summary>Gets Winner or Loser applied to each fixture of the round.</summary>
    public ProgressionOutcome Outcome { get; }

    /// <summary>Gets the destination stage applied to every expanded path.</summary>
    public StageId DestinationStageId { get; }

    /// <summary>
    /// Gets Place destination slot keys (fixture index ↔ key index). Empty = Population.
    /// </summary>
    public IReadOnlyList<string> DestinationSlotKeys { get; }

    /// <summary>Gets a value indicating whether this intent targets population only.</summary>
    public bool TargetsPopulation => DestinationSlotKeys.Count == 0;

    /// <summary>Returns a deep copy.</summary>
    public ProgressionIntent Copy() =>
        new(Id, Order, RoundId, Outcome, DestinationStageId, DestinationSlotKeys);

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
                    "Progression place destination slot keys cannot be empty.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            var slotKey = ProgressionDestination.ForSlot(destinationStageId, key).SlotKey!;
            if (!seen.Add(slotKey))
            {
                throw new DomainException(
                    "Progression place destination slot keys must be unique within an intent.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            normalized[i] = slotKey;
        }

        return normalized;
    }
}
