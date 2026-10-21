// -----------------------------------------------------------------------
// <copyright file="DrawResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Typed resolution outcome of a Draw (NotResolved / Resolved / NoSolution).
/// Not a generic Assignment collection.
/// </summary>
public sealed record DrawResolution
{
    private readonly SlotDrawPlacement[] _slotResults;
    private readonly GroupDrawPlacement[] _groupResults;

    private DrawResolution(
        DrawResolutionState state,
        DrawResolutionKind? resolvedKind,
        IReadOnlyList<SlotDrawPlacement>? slotResults,
        IReadOnlyList<GroupDrawPlacement>? groupResults)
    {
        if (!Enum.IsDefined(state))
        {
            throw new DomainException(
                "Draw resolution state is unknown.",
                StageErrorCodes.DrawResolutionInvalid);
        }

        State = state;
        ResolvedKind = resolvedKind;
        _slotResults = slotResults is null ? [] : [.. slotResults];
        _groupResults = groupResults is null ? [] : [.. groupResults];
    }

    /// <summary>
    /// Gets the resolution state.
    /// </summary>
    public DrawResolutionState State { get; }

    /// <summary>
    /// Gets the kind of a Resolved payload; otherwise <see langword="null"/>.
    /// </summary>
    public DrawResolutionKind? ResolvedKind { get; }

    /// <summary>
    /// Gets Slot results when resolved as Slot.
    /// </summary>
    public IReadOnlyList<SlotDrawPlacement> SlotResults => _slotResults;

    /// <summary>
    /// Gets Group results when resolved as Group.
    /// </summary>
    public IReadOnlyList<GroupDrawPlacement> GroupResults => _groupResults;

    /// <summary>
    /// Creates a not-resolved resolution.
    /// </summary>
    public static DrawResolution NotResolved() =>
        new(DrawResolutionState.NotResolved, null, null, null);

    /// <summary>
    /// Creates a no-solution resolution.
    /// </summary>
    public static DrawResolution NoSolution() =>
        new(DrawResolutionState.NoSolution, null, null, null);

    /// <summary>
    /// Creates a resolved Slot resolution.
    /// </summary>
    public static DrawResolution ResolvedSlots(IReadOnlyList<SlotDrawPlacement> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return results.Count == 0
            ? throw new DomainException(
                "Resolved slot results cannot be empty.",
                StageErrorCodes.DrawResolutionInvalid)
            : results.Select(r => r.EntryId).Distinct().Count() != results.Count
            || results.Select(r => r.SlotKey).Distinct(StringComparer.Ordinal).Count() != results.Count
            ? throw new DomainException(
                "Slot resolution results must have unique entries and unique slot keys.",
                StageErrorCodes.DrawResolutionInvalid)
            : new DrawResolution(DrawResolutionState.Resolved, DrawResolutionKind.Slot, results, null);
    }

    /// <summary>
    /// Creates a resolved Group resolution.
    /// </summary>
    public static DrawResolution ResolvedGroups(IReadOnlyList<GroupDrawPlacement> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return results.Count == 0
            ? throw new DomainException(
                "Resolved group results cannot be empty.",
                StageErrorCodes.DrawResolutionInvalid)
            : results.Select(r => r.EntryId).Distinct().Count() != results.Count
            ? throw new DomainException(
                "Group resolution results must have unique entries.",
                StageErrorCodes.DrawResolutionInvalid)
            : new DrawResolution(DrawResolutionState.Resolved, DrawResolutionKind.Group, null, results);
    }

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public DrawResolution Copy() =>
        new(State, ResolvedKind, _slotResults, _groupResults);
}
