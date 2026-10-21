// -----------------------------------------------------------------------
// <copyright file="DrawGenerationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Flat input contract for <see cref="DrawResolutionGenerator"/> (never the Draw aggregate).
/// Slot: Required = feasibility. Group: pots + uniform capacity + FixedGroups; Required feasibility only (no soft).
/// Incomplete maps / structural incoherence → Invalid regardless of enforcement.
/// </summary>
public sealed class DrawGenerationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawGenerationRequest"/> class.
    /// </summary>
    public DrawGenerationRequest(
        DrawResolutionKind kind,
        IReadOnlyList<EntryId> entries,
        IReadOnlyList<DrawConstraint> constraints,
        DrawConstraintContext constraintContext,
        IRandomSource randomSource,
        IReadOnlyList<string>? targets = null,
        IReadOnlyList<SlotDrawPlacement>? fixedSlots = null,
        IReadOnlyList<GroupId>? groupTargets = null,
        int? numberOfPots = null,
        PotMembership? potMembership = null,
        IReadOnlyList<GroupDrawPlacement>? fixedGroups = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(constraintContext);
        ArgumentNullException.ThrowIfNull(randomSource);

        Kind = kind;
        Entries = entries;
        Constraints = constraints;
        ConstraintContext = constraintContext;
        RandomSource = randomSource;
        Targets = targets;
        FixedSlots = fixedSlots ?? [];
        GroupTargets = groupTargets;
        NumberOfPots = numberOfPots;
        PotMembership = potMembership;
        FixedGroups = fixedGroups ?? [];
    }

    /// <summary>
    /// Gets the resolution kind to generate.
    /// </summary>
    public DrawResolutionKind Kind { get; }

    /// <summary>
    /// Gets the entry pool.
    /// </summary>
    public IReadOnlyList<EntryId> Entries { get; }

    /// <summary>
    /// Gets draw constraints (Required = feasibility).
    /// Unused for Group soft costs (must be empty or MaxSameAssociation only).
    /// </summary>
    public IReadOnlyList<DrawConstraint> Constraints { get; }

    /// <summary>
    /// Gets auxiliary constraint maps (Group association).
    /// </summary>
    public DrawConstraintContext ConstraintContext { get; }

    /// <summary>
    /// Gets the random source (Domain knows only <see cref="IRandomSource"/>).
    /// </summary>
    public IRandomSource RandomSource { get; }

    /// <summary>
    /// Gets Slot target keys when <see cref="Kind"/> is Slot; otherwise unused.
    /// </summary>
    public IReadOnlyList<string>? Targets { get; }

    /// <summary>
    /// Gets fixed Slot placements (Slot kind).
    /// </summary>
    public IReadOnlyList<SlotDrawPlacement> FixedSlots { get; }

    /// <summary>
    /// Gets explicit Group destination ids when <see cref="Kind"/> is Group.
    /// </summary>
    public IReadOnlyList<GroupId>? GroupTargets { get; }

    /// <summary>
    /// Gets <see cref="Rules.PotRules.NumberOfPots"/> for Group generation (required).
    /// </summary>
    public int? NumberOfPots { get; }

    /// <summary>
    /// Gets Entry→pot membership for Group generation (required).
    /// </summary>
    public PotMembership? PotMembership { get; }

    /// <summary>
    /// Gets fixed Group placements (Group kind).
    /// </summary>
    public IReadOnlyList<GroupDrawPlacement> FixedGroups { get; }
}
