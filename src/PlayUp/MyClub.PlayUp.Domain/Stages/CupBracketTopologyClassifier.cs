// -----------------------------------------------------------------------
// <copyright file="CupBracketTopologyClassifier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Classifies whether a stage may receive automatic canonical <see cref="BracketPair"/> repair.
/// </summary>
public static class CupBracketTopologyClassifier
{
    /// <summary>
    /// Classifies <paramref name="stage"/> for canonical mono-round Cup bracket maintenance.
    /// </summary>
    public static CupBracketTopologyKind Classify(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (!IsMonoRoundKnockout(stage))
        {
            return CupBracketTopologyKind.NotApplicable;
        }

        if (!TryParseCanonicalSlotSequence(stage, out var slotCount))
        {
            return CupBracketTopologyKind.ExplicitTopology;
        }

        if (slotCount < 2 || !IsPowerOfTwo(slotCount))
        {
            return CupBracketTopologyKind.InvalidCanonicalShape;
        }

        var slotKeys = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        var expected = BracketPair.CreateEntryRoundPairs(slotKeys);
        return !ExistingPairsAlignWithExpected(stage, expected) ? CupBracketTopologyKind.ExplicitTopology : stage.BracketPairs.Count == expected.Count ? CupBracketTopologyKind.CanonicalComplete : CupBracketTopologyKind.CanonicalIncomplete;
    }

    /// <summary>
    /// Expected entry-round pairs for a validated canonical mono-round stage, or empty when not applicable.
    /// </summary>
    public static IReadOnlyList<BracketPair> ExpectedCanonicalPairs(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (Classify(stage) is not (
            CupBracketTopologyKind.CanonicalComplete
            or CupBracketTopologyKind.CanonicalIncomplete))
        {
            return [];
        }

        var slotKeys = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        return BracketPair.CreateEntryRoundPairs(slotKeys);
    }

    private static bool IsMonoRoundKnockout(Stage stage) =>
        stage is { IsSwiss: false, Rounds.Count: 1, Groups.Count: 0, Matchdays.Count: 0 };

    private static bool TryParseCanonicalSlotSequence(Stage stage, out int slotCount)
    {
        slotCount = 0;
        if (stage.Slots.Count == 0)
        {
            return false;
        }

        var expectedNumber = 1;
        foreach (var slot in stage.Slots)
        {
            if (!TryParseBuildCupSlotKey(slot.SlotKey, out var number) || number != expectedNumber)
            {
                slotCount = 0;
                return false;
            }

            expectedNumber++;
        }

        slotCount = stage.Slots.Count;
        return true;
    }

    private static bool TryParseBuildCupSlotKey(string slotKey, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(slotKey) || slotKey[0] != 'S' || slotKey.Length < 2)
        {
            return false;
        }

        var suffix = slotKey[1..];
        return int.TryParse(suffix, out number) && number >= 1;
    }

    private static bool ExistingPairsAlignWithExpected(
        Stage stage,
        IReadOnlyList<BracketPair> expected)
    {
        var expectedByKey = expected.ToDictionary(pair => pair.PairKey, StringComparer.Ordinal);
        foreach (var existing in stage.BracketPairs)
        {
            if (!expectedByKey.TryGetValue(existing.PairKey, out var match))
            {
                return false;
            }

            if (!string.Equals(existing.SlotAKey, match.SlotAKey, StringComparison.Ordinal)
                || !string.Equals(existing.SlotBKey, match.SlotBKey, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
