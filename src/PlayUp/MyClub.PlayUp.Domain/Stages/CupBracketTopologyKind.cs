// -----------------------------------------------------------------------
// <copyright file="CupBracketTopologyKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Read-side classification for canonical mono-round Cup bracket-pair maintenance.
/// </summary>
public enum CupBracketTopologyKind
{
    /// <summary>Outside mono-round canonical policy (multi-round, groups, championship shape, unstructured).</summary>
    NotApplicable = 0,

    /// <summary>Mono-round Cup with non-canonical naming or misaligned existing pairs — no automatic repair.</summary>
    ExplicitTopology = 1,

    /// <summary>Canonical <c>S1…Sn</c> naming but invalid for a standard entry bracket (e.g. odd <c>n</c>, not a power of two).</summary>
    InvalidCanonicalShape = 2,

    /// <summary>Canonical, valid shape, all expected pairs present.</summary>
    CanonicalComplete = 3,

    /// <summary>Canonical, valid shape, existing pairs aligned — missing pairs may be added without ambiguity.</summary>
    CanonicalIncomplete = 4,
}
