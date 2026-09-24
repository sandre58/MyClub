// -----------------------------------------------------------------------
// <copyright file="BracketPair.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Structural Cup confrontation potential between two Places (distinct from a materialized Fixture).
/// </summary>
/// <remarks>
/// V1 mono-round: PairKey is a persistent business identity (P1, P2, …), never recomputed by UI layout.
/// </remarks>
public sealed record BracketPair
{
    /// <summary>
    /// Maximum length for <see cref="PairKey"/>.
    /// </summary>
    public const int PairKeyMaxLength = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="BracketPair"/> class.
    /// </summary>
    /// <param name="pairKey">Persistent pair identity (e.g. P1).</param>
    /// <param name="slotAKey">First place key.</param>
    /// <param name="slotBKey">Second place key.</param>
    public BracketPair(string pairKey, string slotAKey, string slotBKey)
    {
        PairKey = NormalizePairKey(pairKey);
        SlotAKey = Slot.NormalizeKey(slotAKey);
        SlotBKey = Slot.NormalizeKey(slotBKey);

        if (string.Equals(SlotAKey, SlotBKey, StringComparison.Ordinal))
        {
            throw new DomainException(
                $"BracketPair '{PairKey}' cannot reference the same slot twice ('{SlotAKey}').",
                StageErrorCodes.InvalidConfiguration);
        }
    }

    /// <summary>Gets the persistent pair identity (e.g. P1).</summary>
    public string PairKey { get; }

    /// <summary>Gets place A.</summary>
    public string SlotAKey { get; }

    /// <summary>Gets place B.</summary>
    public string SlotBKey { get; }

    /// <summary>
    /// Builds V1 mono-round pairs from slots in structural order: (S1,S2)→P1, (S3,S4)→P2, ….
    /// </summary>
    /// <param name="slotKeys">Slot keys in structural order.</param>
    /// <returns>Bracket pairs covering consecutive pairs of slots.</returns>
    public static IReadOnlyList<BracketPair> CreateEntryRoundPairs(IReadOnlyList<string> slotKeys)
    {
        ArgumentNullException.ThrowIfNull(slotKeys);
        if (slotKeys.Count == 0 || slotKeys.Count % 2 != 0)
        {
            throw new DomainException(
                $"Entry-round BracketPairs require a positive even slot count (got {slotKeys.Count}).",
                StageErrorCodes.InvalidConfiguration);
        }

        var pairs = new List<BracketPair>(slotKeys.Count / 2);
        for (var i = 0; i < slotKeys.Count; i += 2)
        {
            var index = (i / 2) + 1;
            pairs.Add(new BracketPair($"P{index}", slotKeys[i], slotKeys[i + 1]));
        }

        return pairs;
    }

    /// <summary>
    /// Normalizes a pair key.
    /// </summary>
    public static string NormalizePairKey(string pairKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pairKey);
        var key = pairKey.Trim();
        return key.Length > PairKeyMaxLength
            ? throw new DomainException(
                $"BracketPair key exceeds max length {PairKeyMaxLength}.",
                StageErrorCodes.InvalidConfiguration)
            : key;
    }

    /// <summary>
    /// Returns whether the fixture slots match this pair (A/B or B/A).
    /// </summary>
    public bool MatchesSlots(string? slotAKey, string? slotBKey)
    {
        if (string.IsNullOrWhiteSpace(slotAKey) || string.IsNullOrWhiteSpace(slotBKey))
        {
            return false;
        }

        var a = Slot.NormalizeKey(slotAKey);
        var b = Slot.NormalizeKey(slotBKey);
        return (string.Equals(a, SlotAKey, StringComparison.Ordinal)
                && string.Equals(b, SlotBKey, StringComparison.Ordinal))
               || (string.Equals(a, SlotBKey, StringComparison.Ordinal)
                   && string.Equals(b, SlotAKey, StringComparison.Ordinal));
    }
}
