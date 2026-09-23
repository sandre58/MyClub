// -----------------------------------------------------------------------
// <copyright file="DrawInputsFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds default <see cref="DrawInputs"/> for Structure → Draw flows (encoding F).
/// </summary>
/// <remarks>
/// Entries = phase <see cref="Stage.CompositionEntries"/> only (fail closed — no Active[] fallback).
/// Placements already present on the form become Fixed* ( ⊆ Entries ). The generator free pool is
/// Entries \ Fixed* — not a shrink responsibility of this factory.
/// </remarks>
public static class DrawInputsFactory
{
    /// <summary>
    /// Builds inputs for the draw kind from the stage composition (encoding F).
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="kind">Draw resolution kind.</param>
    public static DrawInputs CreateDefault(Stage stage, DrawResolutionKind kind)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var entries = ResolveComposition(stage);

        return kind switch
        {
            DrawResolutionKind.Group => DrawInputs.ForGroup(
                entries,
                potMembership: BuildSequentialPots(entries, stage),
                fixedPlacements: CollectFixedGroups(stage, entries)),
            DrawResolutionKind.Pairing => DrawInputs.ForPairing(entries),
            DrawResolutionKind.Slot => DrawInputs.ForSlot(
                entries,
                fixedPlacements: CollectFixedSlots(stage, entries)),
            _ => throw new ApplicationFailureException(
                $"Unsupported draw kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static List<EntryId> ResolveComposition(Stage stage) =>
        stage.CompositionEntries.Count == 0
            ? throw new ApplicationFailureException(
                "Draw inputs require a non-empty phase population (CompositionEntries). Active competition entries are not a fallback.",
                ApplicationErrorCodes.DrawGenerationFailure)
            : [.. stage.CompositionEntries.Select(entry => entry.EntryId)];

    private static List<GroupDrawPlacement> CollectFixedGroups(
        Stage stage,
        IReadOnlyList<EntryId> entries)
    {
        var pool = entries.ToHashSet();
        var fixedPlacements = new List<GroupDrawPlacement>();
        foreach (var group in stage.Groups)
        {
            fixedPlacements.AddRange(from entryId in @group.EntryIds where pool.Contains(entryId) select new GroupDrawPlacement(entryId, @group.Id));
        }

        return fixedPlacements;
    }

    private static List<SlotDrawPlacement> CollectFixedSlots(
        Stage stage,
        IReadOnlyList<EntryId> entries)
    {
        var pool = entries.ToHashSet();
        var fixedPlacements = new List<SlotDrawPlacement>();
        foreach (var slot in stage.Slots)
        {
            if (slot.EntryId is { } entryId && pool.Contains(entryId))
            {
                fixedPlacements.Add(new SlotDrawPlacement(entryId, slot.SlotKey));
            }
        }

        return fixedPlacements;
    }

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
