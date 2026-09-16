// -----------------------------------------------------------------------
// <copyright file="DrawInputsFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds default <see cref="DrawInputs"/> for Structure → Draw flows.
/// </summary>
/// <remarks>
/// O4-a: when the stage has a non-empty phase population (<see cref="Stage.CompositionEntries"/>),
/// that set is the draw pool. Otherwise falls back to competition Active entries (legacy stages).
/// </remarks>
public static class DrawInputsFactory
{
    /// <summary>
    /// Builds inputs for the draw kind from phase population or active competition entries.
    /// </summary>
    public static DrawInputs CreateDefault(Competition competition, Stage stage, DrawResolutionKind kind)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        var entries = ResolvePool(competition, stage);

        return entries.Count == 0
            ? throw new ApplicationFailureException(
                "Draw inputs require at least one entry in the phase population (or active competition entries when population is empty).",
                ApplicationErrorCodes.DrawGenerationFailure)
            : kind switch
        {
            DrawResolutionKind.Group => DrawInputs.ForGroup(entries, potMembership: BuildSequentialPots(entries, stage)),
            DrawResolutionKind.Pairing => DrawInputs.ForPairing(entries),
            DrawResolutionKind.Slot => DrawInputs.ForSlot(entries),
            _ => throw new ApplicationFailureException(
                $"Unsupported draw kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static List<EntryId> ResolvePool(Competition competition, Stage stage) =>
        stage.CompositionEntries.Count > 0
            ? [.. stage.CompositionEntries.Select(entry => entry.EntryId)]
            : [
                .. competition.Entries
                    .Where(entry => entry.Status == EntryStatus.Active)
                    .Select(entry => entry.Id)
                    .OrderBy(id => id.Value)
            ];

    private static PotMembership BuildSequentialPots(List<EntryId> entries, Stage stage)
    {
        var numberOfPots = stage.Regulation.DrawRules?.PotRules?.NumberOfPots
            ?? throw new ApplicationFailureException(
                "Group draw requires PotRules on the stage regulation.",
                ApplicationErrorCodes.DrawGenerationFailure);

        if (entries.Count % numberOfPots != 0)
        {
            throw new ApplicationFailureException(
                $"Entry pool count ({entries.Count}) must be divisible by NumberOfPots ({numberOfPots}).",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        // Sequential fill: pot capacity = entries / pots; each pot gets that many consecutive entries.
        var capacity = entries.Count / numberOfPots;
        var map = new Dictionary<EntryId, int>();
        for (var index = 0; index < entries.Count; index++)
        {
            map[entries[index]] = (index / capacity) + 1;
        }

        return new PotMembership(map);
    }
}
