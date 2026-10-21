// -----------------------------------------------------------------------
// <copyright file="CupPrepareBracketReadiness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Canonical mono-round Cup Po2 readiness for Prepare (A1 R2 only).
/// </summary>
internal static class CupPrepareBracketReadiness
{
    internal static bool ShouldSurfaceCupBracketInvalidBlocker(Stage stage)
    {
        if (StructureSkeleton.InferFormat(stage) != StructureFormatKind.Cup)
        {
            return false;
        }

        var regime = CupPrepareBracketRegimeResolver.Resolve(stage);
        return regime switch
        {
            CupPrepareBracketRegime.R2Canonical => RejectsCanonicalPowerOfTwo(stage, regime),
            CupPrepareBracketRegime.R3InvalidCanonical => true,
            CupPrepareBracketRegime.NotApplicable => RejectsLegacyCupEntryPlaces(stage),
            _ => false
        };
    }

    internal static bool RejectsCanonicalPowerOfTwo(Stage stage, CupPrepareBracketRegime regime)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (regime != CupPrepareBracketRegime.R2Canonical)
        {
            return false;
        }

        var cupEntryPlaces = ResolveCupEntryPlaces(stage);
        return cupEntryPlaces is null or < 2 || !IsPowerOfTwo(cupEntryPlaces.Value);
    }

    /// <summary>
    /// Same entry-place rule as <see cref="StructureViewAssembler"/> readiness (mono-round: slot count).
    /// </summary>
    private static bool RejectsLegacyCupEntryPlaces(Stage stage)
    {
        var cupEntryPlaces = ResolveCupEntryPlaces(stage);
        return cupEntryPlaces is null or < 2 || !IsPowerOfTwo(cupEntryPlaces.Value);
    }

    private static int? ResolveCupEntryPlaces(Stage stage)
    {
        var slotCount = stage.Slots.Count;
        if (slotCount == 0)
        {
            return null;
        }

        var roundCount = stage.Rounds.Count;
        if (roundCount <= 1)
        {
            return slotCount;
        }

        var fullTreeSlots = (1 << (roundCount + 1)) - 2;
        if (slotCount == fullTreeSlots)
        {
            return 1 << roundCount;
        }

        var firstRoundSlots = 1 << roundCount;
        if (slotCount == firstRoundSlots)
        {
            return firstRoundSlots;
        }

        if (slotCount < 2 || (slotCount + 2) % 2 != 0)
        {
            return null;
        }

        var entryPlaces = (slotCount + 2) / 2;
        return IsPowerOfTwo(entryPlaces) && entryPlaces >= 2 ? entryPlaces : null;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
