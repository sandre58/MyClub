// -----------------------------------------------------------------------
// <copyright file="DrawGenerationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyNet.Generator;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Flat input contract for <see cref="DrawResolutionGenerator"/> (never the Draw aggregate).
/// Required = feasibility; Preferred = soft optimization (Pairing SameGroup/SameTeam). Application
/// should filter kind-inapplicable constraints before calling; incomplete maps → Invalid regardless of enforcement.
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
        IReadOnlyList<PairingDrawResult>? fixedPairings = null)
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
        FixedPairings = fixedPairings ?? [];
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
    /// Gets draw constraints (Required = feasibility; Preferred = soft cost for Pairing).
    /// </summary>
    public IReadOnlyList<DrawConstraint> Constraints { get; }

    /// <summary>
    /// Gets auxiliary constraint maps.
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
    /// Gets fixed Pairings (Pairing kind).
    /// </summary>
    public IReadOnlyList<PairingDrawResult> FixedPairings { get; }
}
