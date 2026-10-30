// -----------------------------------------------------------------------
// <copyright file="ResolutionProvenanceKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Persisted provenance category for a team resolution (slot, group, or population).
/// </summary>
public enum ResolutionProvenanceKind
{
    /// <summary>
    /// Resolved via qualification Apply from a standing selection.
    /// </summary>
    Qualification = 1,

    /// <summary>
    /// Resolved via progression Apply from a fixture outcome.
    /// </summary>
    Progression = 2,

    /// <summary>
    /// Configured direct slot assignment (not an Apply resolution).
    /// </summary>
    Directe = 3,

    /// <summary>
    /// Occupant placed by a published draw Apply.
    /// </summary>
    Tirage = 4,

    /// <summary>
    /// Explicit non-reconstructible provenance (historical rattrapage or ambiguous feed).
    /// </summary>
    NonVerifiee = 5,
}
