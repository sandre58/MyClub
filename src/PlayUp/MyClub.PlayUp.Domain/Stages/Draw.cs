// -----------------------------------------------------------------------
// <copyright file="Draw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Draw procedure owned by a Stage: selects one admissible resolution under constraints.
/// Lifecycle (<see cref="DrawStatus"/>) is orthogonal to resolution state.
/// Does not create Match, Round, Group, Slot, or Fixture structure.
/// </summary>
[DebuggerDisplay("{Kind} ({Status}/{Resolution.State})")]
public sealed class Draw : Entity<DrawId>
{
    internal Draw(DrawId id, DrawResolutionKind kind)
        : base(id)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(
                "Draw resolution kind is unknown.",
                StageErrorCodes.DrawInvalid);
        }

        Kind = kind;
        Status = DrawStatus.Draft;
        Resolution = DrawResolution.NotResolved();
    }

    /// <summary>
    /// Gets the immutable resolution kind for this draw.
    /// </summary>
    public DrawResolutionKind Kind { get; }

    /// <summary>
    /// Gets the draw lifecycle status.
    /// </summary>
    public DrawStatus Status { get; private set; }

    /// <summary>
    /// Gets configured inputs when present.
    /// </summary>
    public DrawInputs? Inputs { get; private set; }

    /// <summary>
    /// Gets the current resolution.
    /// </summary>
    public DrawResolution Resolution { get; private set; }

    /// <summary>
    /// Configures inputs (Draft only).
    /// </summary>
    internal void ConfigureInputs(DrawInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        EnsureDraftMutable();
        inputs.EnsureCompatibleWith(Kind);
        Inputs = inputs.Copy();

        // Re-configuring clears a prior attempt so resolution stays consistent with inputs.
        Resolution = DrawResolution.NotResolved();
    }

    /// <summary>
    /// Records a typed resolution matching <see cref="Kind"/> (Draft only).
    /// </summary>
    internal void RecordResolution(DrawResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        EnsureDraftMutable();

        if (resolution.State != DrawResolutionState.Resolved
            || resolution.ResolvedKind != Kind)
        {
            throw new DomainException(
                "Resolution kind does not match the draw kind.",
                StageErrorCodes.DrawResolutionKindMismatch);
        }

        EnsureResultsWithinPool(resolution);
        EnsureFixedPlacementsRespected(resolution);
        Resolution = resolution.Copy();
    }

    /// <summary>
    /// Marks that no admissible solution exists (Draft only; ≠ Cancel).
    /// </summary>
    internal void MarkNoSolution()
    {
        EnsureDraftMutable();
        Resolution = DrawResolution.NoSolution();
    }

    /// <summary>
    /// Publishes an immutable resolved draw.
    /// </summary>
    internal void Publish()
    {
        if (Status != DrawStatus.Draft)
        {
            throw new DomainException(
                "Only a draft draw can be published.",
                StageErrorCodes.DrawInvalidTransition);
        }

        if (Resolution.State != DrawResolutionState.Resolved)
        {
            throw new DomainException(
                "A draw can be published only when resolution is Resolved.",
                StageErrorCodes.DrawInvalidTransition);
        }

        if (Inputs is null)
        {
            throw new DomainException(
                "A draw requires configured inputs before publish.",
                StageErrorCodes.DrawInvalidTransition);
        }

        Status = DrawStatus.Published;
    }

    /// <summary>
    /// Cancels the draw (Draft or Published). Cancelled draws cannot be published.
    /// </summary>
    internal void Cancel()
    {
        if (Status == DrawStatus.Cancelled)
        {
            return;
        }

        if (Status is not (DrawStatus.Draft or DrawStatus.Published))
        {
            throw new DomainException(
                "Draw cannot be cancelled from the current status.",
                StageErrorCodes.DrawInvalidTransition);
        }

        Status = DrawStatus.Cancelled;
    }

    /// <summary>
    /// Builds slot assignment instructions from a published Slot draw (no Stage mutation).
    /// </summary>
    public IReadOnlyList<SlotAssignmentInstruction> ToSlotAssignmentInstructions(StageId stageId) =>
        Status != DrawStatus.Published
        || Kind != DrawResolutionKind.Slot
        || Resolution.State != DrawResolutionState.Resolved
            ? throw new DomainException(
                "Slot assignment instructions require a published Slot draw with a resolved result.",
                StageErrorCodes.DrawInvalidTransition)
            : [
                ..Resolution.SlotResults.Select(r =>
                    new SlotAssignmentInstruction(stageId, r.SlotKey, r.EntryId))
            ];

    private void EnsureDraftMutable()
    {
        if (Status != DrawStatus.Draft)
        {
            throw new DomainException(
                "Draw inputs and resolution can only change while Draft.",
                StageErrorCodes.DrawImmutable);
        }
    }

    private void EnsureResultsWithinPool(DrawResolution resolution)
    {
        if (Inputs is null)
        {
            throw new DomainException(
                "Draw inputs must be configured before recording a resolution.",
                StageErrorCodes.DrawInputsInvalid);
        }

        var pool = Inputs.Entries;
        switch (Kind)
        {
            case DrawResolutionKind.Slot:
                if (resolution.SlotResults.Any(result => !pool.Contains(result.EntryId)))
                {
                    throw new DomainException(
                        "Slot resolution references an entry outside the draw pool.",
                        StageErrorCodes.DrawResolutionInvalid);
                }

                break;
            case DrawResolutionKind.Group:
                if (resolution.GroupResults.Any(result => !pool.Contains(result.EntryId)))
                {
                    throw new DomainException(
                        "Group resolution references an entry outside the draw pool.",
                        StageErrorCodes.DrawResolutionInvalid);
                }

                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private void EnsureFixedPlacementsRespected(DrawResolution resolution)
    {
        // Inputs already required by EnsureResultsWithinPool.
        var inputs = Inputs!;

        switch (Kind)
        {
            case DrawResolutionKind.Slot:
                if (inputs.FixedSlots.Any(fixedPlacement => !resolution.SlotResults.Any(r =>
                        r.EntryId.Equals(fixedPlacement.EntryId)
                        && string.Equals(r.SlotKey, fixedPlacement.SlotKey, StringComparison.Ordinal))))
                {
                    throw new DomainException(
                        "Slot resolution must include all configured fixed placements.",
                        StageErrorCodes.DrawFixedPlacementViolation);
                }

                break;
            case DrawResolutionKind.Group:
                if (inputs.FixedGroups.Any(fixedPlacement => !resolution.GroupResults.Any(r =>
                        r.EntryId.Equals(fixedPlacement.EntryId)
                        && r.GroupId.Equals(fixedPlacement.GroupId))))
                {
                    throw new DomainException(
                        "Group resolution must include all configured fixed placements.",
                        StageErrorCodes.DrawFixedPlacementViolation);
                }

                break;
            default:
                throw new InvalidOperationException();
        }
    }
}
