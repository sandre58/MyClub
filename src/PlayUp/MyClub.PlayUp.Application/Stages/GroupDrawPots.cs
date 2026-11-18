// -----------------------------------------------------------------------
// <copyright file="GroupDrawPots.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared pot calculation (A2). The Group launch gate and the Group rerun (lot C) must call this function as-is.
/// </summary>
public static class GroupDrawPots
{
    /// <summary>
    /// Sequential pots for an ordered entry list. Pot capacity is <c>entries / numberOfPots</c>;
    /// each pot receives that many consecutive entries. Pot numbers start at 1.
    /// </summary>
    /// <param name="entries">Composition order. The same order <see cref="DrawInputsFactory"/> passes for a Group draw.</param>
    /// <param name="numberOfPots">Regulation pot count.</param>
    /// <returns>Entry to pot number.</returns>
    /// <exception cref="ApplicationFailureException">
    /// The entry count is not divisible by <paramref name="numberOfPots"/>
    /// (<see cref="ApplicationErrorCodes.DrawGenerationFailure"/>).
    /// </exception>
    public static IReadOnlyDictionary<EntryId, int> Compute(
        IReadOnlyList<EntryId> entries,
        int numberOfPots)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count % numberOfPots != 0)
        {
            throw new ApplicationFailureException(
                $"Entry pool count ({entries.Count}) must be divisible by NumberOfPots ({numberOfPots}).",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        // Sequential fill: pot capacity = entries / pots; each pot gets that many consecutive entries.
        var capacity = entries.Count / numberOfPots;
        var map = new Dictionary<EntryId, int>(entries.Count);
        for (var index = 0; index < entries.Count; index++)
        {
            map[entries[index]] = (index / capacity) + 1;
        }

        return map;
    }

    /// <summary>
    /// Pots of the current composition. Returns null when draw pot rules are absent, the composition
    /// is empty, or the entry count is not divisible by the pot count. Never throws.
    /// The result is the current composition, not a pot recorded on a past draw.
    /// </summary>
    /// <param name="stage">Phase whose composition and draw rules are read.</param>
    /// <returns>Entry to pot number, or null when pots cannot be computed.</returns>
    public static IReadOnlyDictionary<EntryId, int>? TryRead(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var numberOfPots = stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        if (numberOfPots is null || stage.CompositionEntries.Count == 0)
        {
            return null;
        }

        if (stage.CompositionEntries.Count % numberOfPots.Value != 0)
        {
            return null;
        }

        return Compute(
            stage.CompositionEntries.Select(entry => entry.EntryId).ToArray(),
            numberOfPots.Value);
    }
}
