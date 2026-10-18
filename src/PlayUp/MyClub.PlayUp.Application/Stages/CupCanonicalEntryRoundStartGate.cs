// -----------------------------------------------------------------------
// <copyright file="CupCanonicalEntryRoundStartGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Cup Start guard (decision 1a): canonical mono-round entry pairs must be determined before Running.
/// </summary>
internal static class CupCanonicalEntryRoundStartGate
{
    internal const string DisabledReasonCode = "IncompleteEntryRound";

    internal static bool BlocksStart(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return TryGetCanonicalEntryRoundProgress(stage, out _, out var undetermined)
               && undetermined > 0;
    }

    /// <summary>
    /// Canonical mono-round Cup entry-round progress for Structure workspace advisories (UX tranche 2).
    /// </summary>
    internal static bool TryGetCanonicalEntryRoundProgress(
        Stage stage,
        out int canonicalPairCount,
        out int undeterminedPairCount)
    {
        ArgumentNullException.ThrowIfNull(stage);
        canonicalPairCount = 0;
        undeterminedPairCount = 0;
        var pairs = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        if (pairs.Count == 0)
        {
            return false;
        }

        canonicalPairCount = pairs.Count;
        foreach (var pair in pairs)
        {
            if (!IsPairDetermined(stage, pair))
            {
                undeterminedPairCount++;
            }
        }

        return true;
    }

    private static bool IsPairDetermined(Stage stage, BracketPair pair)
    {
        var slotA = stage.FindSlot(pair.SlotAKey);
        var slotB = stage.FindSlot(pair.SlotBKey);
        return slotA?.EntryId is { } home
               && slotB?.EntryId is { } away
               && !home.Equals(away);
    }
}
