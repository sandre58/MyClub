// -----------------------------------------------------------------------
// <copyright file="DestinationSatisfaction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Shared satisfaction of a Qual/Prog path destination against destination Stage state.
/// SoT = <c>Path.Destination</c> one-of (Population | Form | Group | Slot) — not Instruction.TargetsPopulation.
/// </summary>
internal static class DestinationSatisfaction
{
    internal enum Kind
    {
        Satisfied,
        Pending,
        Conflict
    }

    /// <summary>
    /// Evaluates whether <paramref name="expectedEntry"/> has been materialised for the destination kind.
    /// </summary>
    internal static Kind Evaluate(
        Stage destination,
        EntryId expectedEntry,
        bool targetsPopulation,
        bool targetsForm,
        bool targetsGroup,
        bool targetsSlot,
        string? slotKey,
        GroupId? groupId,
        string? formPathFingerprint) =>
        targetsPopulation
            ? InComposition(destination, expectedEntry) ? Kind.Satisfied : Kind.Pending
            : targetsForm
                ? EvaluateForm(destination, expectedEntry, formPathFingerprint)
                : targetsGroup
                    ? EvaluateGroup(destination, expectedEntry, groupId)
                    : targetsSlot ? EvaluateSlot(destination, expectedEntry, slotKey) : Kind.Pending;

    internal static Kind EvaluateQualification(
        Stage destination,
        StageId sourceStageId,
        QualificationPath path,
        EntryId expectedEntry)
    {
        var formFingerprint = path.Destination.TargetsForm
            ? FormPathResolutionKey.FromQualification(sourceStageId, path)
            : null;
        return Evaluate(
            destination,
            expectedEntry,
            path.Destination.TargetsPopulation,
            path.Destination.TargetsForm,
            path.Destination.TargetsGroup,
            path.Destination.TargetsSlot,
            path.Destination.SlotKey,
            path.Destination.GroupId,
            formFingerprint);
    }

    internal static Kind EvaluateProgression(
        Stage destination,
        StageId sourceStageId,
        ProgressionPath path,
        EntryId expectedEntry)
    {
        var formFingerprint = path.Destination.TargetsForm
            ? FormPathResolutionKey.FromProgression(sourceStageId, path)
            : null;
        return Evaluate(
            destination,
            expectedEntry,
            path.Destination.TargetsPopulation,
            path.Destination.TargetsForm,
            path.Destination.TargetsGroup,
            path.Destination.TargetsSlot,
            path.Destination.SlotKey,
            path.Destination.GroupId,
            formFingerprint);
    }

    private static Kind EvaluateForm(Stage destination, EntryId expectedEntry, string? fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint))
        {
            return Kind.Pending;
        }

        var resolution = destination.FormPathResolutions
            .FirstOrDefault(r => r.PathFingerprint == fingerprint);
        return resolution is null
            ? Kind.Pending
            : !resolution.EntryId.Equals(expectedEntry)
            ? Kind.Conflict
            : InComposition(destination, expectedEntry) ? Kind.Satisfied : Kind.Pending;
    }

    private static Kind EvaluateGroup(Stage destination, EntryId expectedEntry, GroupId? groupId)
    {
        if (groupId is null)
        {
            return Kind.Pending;
        }

        var group = destination.FindGroup(groupId.Value);
        return group is null || !InComposition(destination, expectedEntry)
            ? Kind.Pending
            : group.EntryIds.Contains(expectedEntry) ? Kind.Satisfied : Kind.Pending;
    }

    private static Kind EvaluateSlot(Stage destination, EntryId expectedEntry, string? slotKey)
    {
        if (string.IsNullOrEmpty(slotKey))
        {
            return Kind.Pending;
        }

        var slot = destination.FindSlot(slotKey);
        return slot is null
            ? Kind.Pending
            : slot.EntryId is { } occupant && !occupant.Equals(expectedEntry)
            ? Kind.Conflict
            : !InComposition(destination, expectedEntry) || slot.EntryId is null ? Kind.Pending : Kind.Satisfied;
    }

    private static bool InComposition(Stage destination, EntryId entryId) =>
        destination.CompositionEntries.Any(entry => entry.EntryId.Equals(entryId));
}
