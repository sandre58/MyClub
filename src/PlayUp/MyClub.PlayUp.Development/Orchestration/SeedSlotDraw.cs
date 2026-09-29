// -----------------------------------------------------------------------
// <copyright file="SeedSlotDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Seeds deterministic slot draws and population→slot placement for development scenarios.
/// </summary>
internal static class SeedSlotDraw
{
    public static IReadOnlyList<Match> ApplyCupSlotDrawDeterministic(
        ScenarioContext context,
        Competition competition,
        Stage stage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        EnsureDrawRulesEngaged(stage);

        // Encoding F: pool = CompositionEntries (not Active[]).
        var entries = stage.CompositionEntries
            .Select(entry => entry.EntryId)
            .OrderBy(id => id.Value)
            .ToArray();

        if (entries.Length < 2 || (entries.Length & (entries.Length - 1)) != 0)
        {
            throw new InvalidOperationException(
                $"Cup slot draw on '{stage.Name.Value}' requires a power-of-two Composition count (got {entries.Length}).");
        }

        var slotKeys = stage.Slots
            .Select(slot => slot.SlotKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        if (slotKeys.Length != entries.Length)
        {
            throw new InvalidOperationException(
                $"Cup slot draw on '{stage.Name.Value}' requires slot count ({slotKeys.Length}) to match composition ({entries.Length}).");
        }

        RecordAndApplySlotDraw(context, stage, entries, slotKeys);
        return SeedMaterialization.MaterializeFromSlots(context, competition, stage, pairKeys: null);
    }

    /// <summary>
    /// Seed guard: Structure chrome requires DrawRules before any Create/Publish/Apply Draw.
    /// </summary>
    internal static void EnsureDrawRulesEngaged(Stage stage)
    {
        if (stage.Regulation.DrawRules is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Stage '{stage.Name.Value}' must have DrawRules before creating a draw (seed incoherence).");
    }

    /// <summary>
    /// Engages random DrawRules when absent — call immediately before a seed Create/Apply Draw.
    /// </summary>
    internal static void EngageRandomDrawRules(Stage stage, IClock clock)
    {
        if (stage.Regulation.DrawRules is not null)
        {
            return;
        }

        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), clock);
    }

    /// <summary>
    /// Case 1: after ApplyQualification filled <see cref="Stage.CompositionEntries"/>,
    /// place them into form slots via a deterministic Slot Draw (then Publish + Apply).
    /// </summary>
    internal static void PlacePopulationIntoSlotsViaDraw(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        string[] slotKeys)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(slotKeys);

        var pool = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        if (pool.Length == 0)
        {
            throw new InvalidOperationException(
                $"Stage '{stage.Name.Value}' has no population entries to place into slots.");
        }

        if (pool.Length != slotKeys.Length)
        {
            throw new InvalidOperationException(
                $"Population count ({pool.Length}) must match slot count ({slotKeys.Length}) for stage '{stage.Name.Value}'.");
        }

        _ = competition;
        EngageRandomDrawRules(stage, context.Clock);
        RecordAndApplySlotDraw(context, stage, pool, slotKeys);
    }

    /// <summary>
    /// Case 7: Slot Draw only for vacant Places from Population members not already occupying a Place.
    /// Auto-fed Places stay untouched; Occupants ⊆ Population remains.
    /// </summary>
    internal static void PlaceRemainingPopulationIntoEmptySlotsViaDraw(
        ScenarioContext context,
        Competition competition,
        Stage stage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        var occupiedEntryIds = stage.Slots
            .SelectMany(slot => slot.EntryId is { } entryId ? [entryId] : Array.Empty<EntryId>())
            .ToHashSet();
        var emptySlotKeys = stage.Slots
            .Where(slot => slot.EntryId is null)
            .Select(slot => slot.SlotKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var remainingPool = stage.CompositionEntries
            .Select(entry => entry.EntryId)
            .Where(entryId => !occupiedEntryIds.Contains(entryId))
            .ToArray();

        if (emptySlotKeys.Length == 0)
        {
            return;
        }

        if (remainingPool.Length != emptySlotKeys.Length)
        {
            throw new InvalidOperationException(
                $"Hybrid draw requires remaining population ({remainingPool.Length}) to match empty slots ({emptySlotKeys.Length}) on '{stage.Name.Value}'.");
        }

        EngageRandomDrawRules(stage, context.Clock);
        RecordAndApplySlotDraw(context, stage, remainingPool, emptySlotKeys);
        _ = competition;
    }

    internal static void RecordAndApplySlotDraw(
        ScenarioContext context,
        Stage stage,
        EntryId[] pool,
        string[] slotKeys)
    {
        EnsureDrawRulesEngaged(stage);

        var inputs = DrawInputs.ForSlot(pool);
        var draw = stage.CreateDraw(
            DrawResolutionKind.Slot,
            context.Ids.Draw($"slot-{stage.Id.Value:N}-{slotKeys.Length}"),
            context.Clock);
        stage.ConfigureDrawInputs(draw.Id, inputs);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
                [.. pool.Select((entryId, index) => new SlotDrawPlacement(entryId, slotKeys[index]))]),
            context.Clock);
        stage.PublishDraw(draw.Id, context.Clock);
        ApplyDraw.Execute(stage, draw.Id, context.Clock);
    }
}
