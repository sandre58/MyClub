// -----------------------------------------------------------------------
// <copyright file="StructureResolutionOccupantDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Where a resolution occupant is stored on a phase.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Explicit occupant targets without an unknown sentinel.")]
public enum StructureResolutionOccupantTarget
{
    /// <summary>Slot occupant.</summary>
    Slot = 1,

    /// <summary>Group membership occupant.</summary>
    Group = 2,

    /// <summary>Population (composition) occupant.</summary>
    Population = 3,
}

/// <summary>
/// Server-computed freshness of a stored resolution occupant.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Explicit freshness vocabulary without an unknown sentinel.")]
public enum StructureResolutionFreshness
{
    /// <summary>Stored occupant still matches the current upstream selection.</summary>
    AJour = 1,

    /// <summary>Stored occupant is stale.</summary>
    Perime = 2,

    /// <summary>Selection could not be decided.</summary>
    Indetermine = 3,

    /// <summary>Provenance was explicitly not verified.</summary>
    NonVerifiee = 4,
}

/// <summary>
/// Why a resolution occupant is <see cref="StructureResolutionFreshness.Perime"/>.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Explicit staleness causes without an unknown sentinel.")]
public enum StructureResolutionStalenessCause
{
    /// <summary>Upstream result or draw population no longer selects this occupant.</summary>
    ResultatAmont = 1,

    /// <summary>The qualification rule no longer matches the stored source path.</summary>
    RegleQualification = 2,

    /// <summary>An upstream occupant this resolution depends on is already stale.</summary>
    AmontPerime = 3,
}

/// <summary>
/// Why a resolution occupant is <see cref="StructureResolutionFreshness.Indetermine"/>.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Explicit indeterminacy causes without an unknown sentinel.")]
public enum StructureResolutionIndeterminacyCause
{
    /// <summary>A progression match is not finished.</summary>
    MatchNonJoue = 1,

    /// <summary>A finished confrontation is still level.</summary>
    Egalite = 2,

    /// <summary>The source stage or source path cannot be found.</summary>
    SourceAbsente = 3,

    /// <summary>The standing does not designate an entry.</summary>
    SelectionVide = 4,

    /// <summary>The selection could not be computed.</summary>
    Autre = 5,

    /// <summary>Several teams are tied in the standing.</summary>
    EgaliteClassement = 6,

    /// <summary>The stored team no longer meets the qualification condition.</summary>
    ConditionNonRemplie = 7,

    /// <summary>Source-phase setup threw while the selection was evaluated.</summary>
    ConfigurationInvalide = 8,
}

/// <summary>
/// One stored resolution occupant and its freshness, computed on the Structure overview.
/// </summary>
/// <param name="Target">Slot, group, or population.</param>
/// <param name="EntryId">Stored occupant.</param>
/// <param name="ProvenanceKind">Persisted provenance category.</param>
/// <param name="SourcePathId">Persisted source path identifier.</param>
/// <param name="State">Freshness. Never rewritten for <see cref="ResolutionProvenanceKind.Directe"/> or <see cref="ResolutionProvenanceKind.NonVerifiee"/>.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Slot"/>.</param>
/// <param name="GroupId">Group identity when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Group"/>.</param>
/// <param name="StalenessCause">Cause when <paramref name="State"/> is <see cref="StructureResolutionFreshness.Perime"/>; otherwise null.</param>
/// <param name="UpstreamStageId">Source stage the selection was read from, when known.</param>
/// <param name="IndeterminacyCause">Cause when <paramref name="State"/> is <see cref="StructureResolutionFreshness.Indetermine"/>; otherwise null.</param>
/// <param name="ActionMatchId">Match the organizer should open for an indeterminate progression, when known.</param>
/// <param name="ActionMatchLabel">Entry names of <paramref name="ActionMatchId"/>, when both are known.</param>
/// <param name="CascadeReplaceable">Whether a cascade replace preview can resolve a replay for this occupant.</param>
public sealed record StructureResolutionOccupantDto(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    ResolutionProvenanceKind ProvenanceKind,
    string SourcePathId,
    StructureResolutionFreshness State,
    string? SlotKey = null,
    Guid? GroupId = null,
    StructureResolutionStalenessCause? StalenessCause = null,
    Guid? UpstreamStageId = null,
    StructureResolutionIndeterminacyCause? IndeterminacyCause = null,
    Guid? ActionMatchId = null,
    string? ActionMatchLabel = null,
    bool CascadeReplaceable = false);
