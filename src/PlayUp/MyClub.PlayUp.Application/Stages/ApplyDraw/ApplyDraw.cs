// -----------------------------------------------------------------------
// <copyright file="ApplyDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: apply a published Draw resolution (Slot or Group) onto its owning Stage.
/// </summary>
/// <remarks>
/// Preflights all known failure conditions before any mutation.
/// Persistence: caller loads tracked ARs in one DI scope, invokes this use case, then calls
/// <c>IUnitOfWork.SaveChangesAsync</c> once.
/// Does not recalculate WhoFeeds and never creates DirectAssignment.
/// Group: Apply rematerializes only entries present in the resolution (move from other group if needed);
/// same-group is idempotent; entries outside the resolution are left untouched.
/// Host supplies Draw entry pools (typically ⊆ qualified/progressed Entries).
/// Cup confrontations are created via <see cref="MaterializeCupFromOccupiedSlots"/> after Slot apply.
/// </remarks>
public static class ApplyDraw
{
    /// <summary>
    /// Applies a published resolved Draw onto already-loaded Stage.
    /// </summary>
    /// <param name="stage">Stage that owns the Draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Slot instructions (Group returns empty instructions).</returns>
    public static ApplyDrawResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        EnsureApplicable(draw);

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => new ApplyDrawResult(ApplySlot(stage, draw, clock), []),
            DrawResolutionKind.Group => new ApplyDrawResult(ApplyGroup(stage, draw), []),
            _ => throw new ApplicationFailureException(
                $"ApplyDraw does not support draw kind '{draw.Kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static void EnsureApplicable(Draw draw)
    {
        if (draw.Status != DrawStatus.Published)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Published to apply (status is '{draw.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Resolved to apply (resolution is '{draw.Resolution.State}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Resolution.ResolvedKind != draw.Kind)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' resolution kind does not match draw kind.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Inputs is null)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' has no configured inputs.",
                ApplicationErrorCodes.DrawApplyFailure);
        }
    }

    private static IReadOnlyList<SlotAssignmentInstruction> ApplySlot(
        Stage stage,
        Draw draw,
        IClock clock)
    {
        var instructions = draw.ToSlotAssignmentInstructions(stage.Id);
        var pool = draw.Inputs!.Entries;

        foreach (var instruction in instructions)
        {
            if (!instruction.StageId.Equals(stage.Id))
            {
                throw new ApplicationFailureException(
                    $"Slot instruction targets stage '{instruction.StageId}' but draw belongs to '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            var slot = stage.FindSlot(instruction.SlotKey)
                ?? throw new ApplicationFailureException(
                    $"Draw slot '{instruction.SlotKey}' was not found on stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);

            if (!pool.Contains(instruction.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Entry '{instruction.EntryId}' is outside the draw pool.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (stage.DirectAssignments.Any(a =>
                    string.Equals(a.SlotKey, slot.SlotKey, StringComparison.Ordinal)))
            {
                throw new ApplicationFailureException(
                    $"Slot '{slot.SlotKey}' is owned by a direct assignment and cannot receive a draw resolution.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (slot.EntryId is { } current && !current.Equals(instruction.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Slot '{slot.SlotKey}' already occupied by '{current}'; draw requires '{instruction.EntryId}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        foreach (var instruction in instructions)
        {
            stage.ApplyResolvedEntry(instruction.SlotKey, instruction.EntryId, clock);
        }

        return instructions;
    }

    private static IReadOnlyList<SlotAssignmentInstruction> ApplyGroup(
        Stage stage,
        Draw draw)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Group draw cannot be applied when stage structure is locked (status '{stage.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var placements = draw.Resolution.GroupResults;
        var pool = draw.Inputs!.Entries;

        // Validate first — no mutation until every placement is applicable.
        foreach (var placement in placements)
        {
            _ = stage.FindGroup(placement.GroupId)
                ?? throw new ApplicationFailureException(
                    $"Draw group '{placement.GroupId}' was not found on stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);

            if (!pool.Contains(placement.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Entry '{placement.EntryId}' is outside the draw pool.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        // Targeted rematerialization: only entries in this resolution move.
        // Same-group → no-op; other-group → remove then assign; unassigned → assign.
        foreach (var placement in placements)
        {
            var owningGroup = stage.Groups.FirstOrDefault(g => g.EntryIds.Contains(placement.EntryId));
            if (owningGroup?.Id.Equals(placement.GroupId) == false)
            {
                stage.RemoveEntryFromGroup(owningGroup.Id, placement.EntryId);
            }

            stage.AssignEntryToGroup(placement.GroupId, placement.EntryId);
        }

        return [];
    }
}
