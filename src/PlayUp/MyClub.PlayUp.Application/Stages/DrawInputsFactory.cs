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
/// <see cref="DrawInputsIntent.Default"/>: placements already present on the form become Fixed*
/// ( ⊆ Entries ). The generator free pool is Entries \ Fixed*.
/// <see cref="DrawInputsIntent.Rerun"/>: Fixed* empty — full reshuffle from CompositionEntries
/// without promoting current occupancy (Cancel does not clear the phase).
/// <see cref="CreatePartial"/>: unplaced teams only, no Fixed* — occupants stay outside the draw.
/// </remarks>
public static class DrawInputsFactory
{
    /// <summary>
    /// Builds inputs for the draw kind from the stage composition.
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="kind">Draw resolution kind.</param>
    /// <param name="intent">Default keeps occupancy Fixed*; Rerun is a full redraw without them.</param>
    public static DrawInputs CreateDefault(
        Stage stage,
        DrawResolutionKind kind,
        DrawInputsIntent intent = DrawInputsIntent.Default)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var entries = ResolveComposition(stage);
        var promoteFixed = intent == DrawInputsIntent.Default;

        return kind switch
        {
            DrawResolutionKind.Group => DrawInputs.ForGroup(
                entries,
                potMembership: BuildSequentialPots(entries, stage),
                fixedPlacements: promoteFixed ? CollectFixedGroups(stage, entries) : []),
            DrawResolutionKind.Slot => DrawInputs.ForSlot(
                entries,
                fixedPlacements: promoteFixed ? CollectFixedSlots(stage, entries) : []),
            _ => throw new ApplicationFailureException(
                $"Unsupported draw kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    /// <summary>
    /// Builds Slot inputs that draw only unplaced teams. Refuses when free slots and unplaced teams differ.
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="competitionStages">Competition stages used to resolve inbound feeds.</param>
    /// <param name="kind">Must be <see cref="DrawResolutionKind.Slot"/>.</param>
    /// <returns>Inputs whose pool is the unplaced composition entries and whose fixed list is empty.</returns>
    public static DrawInputs CreatePartial(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        DrawResolutionKind kind)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (kind != DrawResolutionKind.Slot)
        {
            throw new ApplicationFailureException(
                $"Partial draw supports Slot only (kind is '{kind}').",
                ApplicationErrorCodes.DrawKindNotSupported);
        }

        var scope = SlotPartialDrawPlanner.Compose(stage, competitionStages);
        SlotPartialDrawPlanner.EnsureDrawable(scope);
        return DrawInputs.ForSlot(scope.UnplacedEntries);
    }

    /// <summary>
    /// Builds Rerun inputs from the applied draw's recorded pool, keeping only entries still
    /// present in <see cref="Stage.CompositionEntries"/>. Extra composition members that were
    /// added after Apply (stale Tirage) must not widen the Rerun pool beyond the cancelled draw.
    /// </summary>
    public static DrawInputs CreateRerun(Stage stage, Draw appliedDraw)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(appliedDraw);

        if (appliedDraw.Inputs is null)
        {
            throw new ApplicationFailureException(
                $"Draw '{appliedDraw.Id}' has no inputs to rerun.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        var entries = appliedDraw.Inputs.Entries.Where(composition.Contains).ToList();
        if (entries.Count == 0)
        {
            throw new ApplicationFailureException(
                "Rerun requires at least one entry from the applied draw still in the phase population.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        return appliedDraw.Kind switch
        {
            DrawResolutionKind.Slot => DrawInputs.ForSlot(entries),
            DrawResolutionKind.Group => DrawInputs.ForGroup(
                entries,
                potMembership: FilterPotMembership(appliedDraw.Inputs.PotMembership, entries)),
            _ => throw new ApplicationFailureException(
                $"Unsupported draw kind '{appliedDraw.Kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static PotMembership? FilterPotMembership(PotMembership? source, IReadOnlyList<EntryId> entries)
    {
        if (source is null)
        {
            return null;
        }

        var pool = entries.ToHashSet();
        var filtered = source.Pots.Where(pair => pool.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        return filtered.Count == 0 ? null : new PotMembership(filtered);
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

        return new PotMembership(GroupDrawPots.Compute(entries, numberOfPots));
    }
}
