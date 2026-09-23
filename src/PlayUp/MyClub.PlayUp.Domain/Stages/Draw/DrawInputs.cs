// -----------------------------------------------------------------------
// <copyright file="DrawInputs.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Concrete inputs owned by a Draw (Entries, optional SeedMap / PotMembership / fixed placements).
/// Entry pool is assembled by Host/orchestration (e.g. from Qualification or Progression results);
/// Domain does not enforce that the pool equals a prior mechanism's population.
/// Fixed placements reuse the same placement shapes as resolution results for the Draw kind —
/// not DirectAssignment and not a Fixed*Placement type family.
/// </summary>
public sealed record DrawInputs
{
    private readonly EntryId[] _entries;
    private readonly SlotDrawPlacement[] _fixedSlots;
    private readonly GroupDrawPlacement[] _fixedGroups;

    private DrawInputs(
        IReadOnlyList<EntryId> entries,
        SeedMap? seedMap,
        PotMembership? potMembership,
        IReadOnlyList<SlotDrawPlacement>? fixedSlots,
        IReadOnlyList<GroupDrawPlacement>? fixedGroups)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            throw new DomainException(
                "Draw inputs require at least one entry.",
                StageErrorCodes.DrawInputsInvalid);
        }

        if (entries.Distinct().Count() != entries.Count)
        {
            throw new DomainException(
                "Draw entry list cannot contain duplicates.",
                StageErrorCodes.DrawInputsInvalid);
        }

        _entries = [..entries];
        SeedMap = seedMap?.Copy();
        PotMembership = potMembership?.Copy();
        _fixedSlots = fixedSlots is null ? [] : [..fixedSlots];
        _fixedGroups = fixedGroups is null ? [] : [..fixedGroups];

        ValidateMapsAgainstEntries();
        ValidateFixedAgainstEntries();
    }

    /// <summary>
    /// Gets the entry pool.
    /// </summary>
    public IReadOnlyList<EntryId> Entries => _entries;

    /// <summary>
    /// Gets optional concrete seeds.
    /// </summary>
    public SeedMap? SeedMap { get; }

    /// <summary>
    /// Gets optional pot membership.
    /// </summary>
    public PotMembership? PotMembership { get; }

    /// <summary>
    /// Gets fixed Slot placements (kind Slot only).
    /// </summary>
    public IReadOnlyList<SlotDrawPlacement> FixedSlots => _fixedSlots;

    /// <summary>
    /// Gets fixed Group placements (kind Group only).
    /// </summary>
    public IReadOnlyList<GroupDrawPlacement> FixedGroups => _fixedGroups;

    /// <summary>
    /// Creates inputs for a Slot draw.
    /// </summary>
    public static DrawInputs ForSlot(
        IReadOnlyList<EntryId> entries,
        SeedMap? seedMap = null,
        PotMembership? potMembership = null,
        IReadOnlyList<SlotDrawPlacement>? fixedPlacements = null) =>
        new(entries, seedMap, potMembership, fixedPlacements, null);

    /// <summary>
    /// Creates inputs for a Group draw.
    /// </summary>
    public static DrawInputs ForGroup(
        IReadOnlyList<EntryId> entries,
        SeedMap? seedMap = null,
        PotMembership? potMembership = null,
        IReadOnlyList<GroupDrawPlacement>? fixedPlacements = null) =>
        new(entries, seedMap, potMembership, null, fixedPlacements);

    /// <summary>
    /// Ensures fixed placement lists match the draw kind.
    /// </summary>
    public void EnsureCompatibleWith(DrawResolutionKind kind)
    {
        var invalid = kind switch
        {
            DrawResolutionKind.Slot => _fixedGroups.Length > 0,
            DrawResolutionKind.Group => _fixedSlots.Length > 0,
            _ => true
        };

        if (invalid)
        {
            throw new DomainException(
                "Fixed placements do not match the draw resolution kind.",
                StageErrorCodes.DrawInputsInvalid);
        }
    }

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public DrawInputs Copy() =>
        new(_entries, SeedMap, PotMembership, _fixedSlots, _fixedGroups);

    private void ValidateMapsAgainstEntries()
    {
        if (SeedMap is not null)
        {
            if (SeedMap.Seeds.Keys.Any(entryId => !_entries.Contains(entryId)))
            {
                throw new DomainException(
                    "SeedMap contains an entry outside the draw pool.",
                    StageErrorCodes.DrawInputsInvalid);
            }
        }

        if (PotMembership is null) return;

        if (PotMembership.Pots.Keys.Any(entryId => !_entries.Contains(entryId)))
        {
            throw new DomainException(
                "PotMembership contains an entry outside the draw pool.",
                StageErrorCodes.DrawInputsInvalid);
        }
    }

    private void ValidateFixedAgainstEntries()
    {
        if (_fixedSlots.Any(placement => !_entries.Contains(placement.EntryId)))
        {
            throw new DomainException(
                "Fixed slot placement references an entry outside the draw pool.",
                StageErrorCodes.DrawInputsInvalid);
        }

        if (_fixedSlots.Select(p => p.EntryId).Distinct().Count() != _fixedSlots.Length
            || _fixedSlots.Select(p => p.SlotKey).Distinct(StringComparer.Ordinal).Count() != _fixedSlots.Length)
        {
            throw new DomainException(
                "Fixed slot placements must have unique entries and unique slot keys.",
                StageErrorCodes.DrawInputsInvalid);
        }

        if (_fixedGroups.Any(placement => !_entries.Contains(placement.EntryId)))
        {
            throw new DomainException(
                "Fixed group placement references an entry outside the draw pool.",
                StageErrorCodes.DrawInputsInvalid);
        }

        if (_fixedGroups.Select(p => p.EntryId).Distinct().Count() != _fixedGroups.Length)
        {
            throw new DomainException(
                "Fixed group placements must have unique entries.",
                StageErrorCodes.DrawInputsInvalid);
        }
    }
}
