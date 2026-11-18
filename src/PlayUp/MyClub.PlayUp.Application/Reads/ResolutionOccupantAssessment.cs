// -----------------------------------------------------------------------
// <copyright file="ResolutionOccupantAssessment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// One stored resolution occupant plus the entry a fresh replay of its path would select.
/// </summary>
/// <param name="StageId">Destination stage.</param>
/// <param name="Target">Where the occupant is stored.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="Kind">Provenance kind.</param>
/// <param name="SourcePathId">Provenance path.</param>
/// <param name="State">Freshness.</param>
/// <param name="Cause">Staleness cause when <paramref name="State"/> is stale.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
/// <param name="DependsOnStageId">Source stage the selection was read from.</param>
/// <param name="DependsOnEntryIds">Entries that selection consulted.</param>
/// <param name="ReplayEntryId">Entry the pure selection returns now, when it resolves.</param>
/// <param name="ReplayFingerprint">Fingerprint to persist for <paramref name="ReplayEntryId"/>.</param>
/// <param name="IndeterminacyCause">Cause when <paramref name="State"/> is indeterminate.</param>
/// <param name="ActionMatchId">Match to open for an indeterminate progression, when known.</param>
/// <param name="ActionMatchLabel">Entry names of <paramref name="ActionMatchId"/>, when both are known.</param>
/// <param name="SourceAbsence">Detail when <paramref name="IndeterminacyCause"/> is a missing source.</param>
/// <param name="ParsedSourceStageId">Stage id read from the source path, when the id parses.</param>
public sealed record ResolutionOccupantAssessment(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    ResolutionProvenanceKind Kind,
    string SourcePathId,
    StructureResolutionFreshness State,
    StructureResolutionStalenessCause? Cause,
    string? SlotKey,
    Guid? GroupId,
    Guid? DependsOnStageId,
    IReadOnlyList<Guid> DependsOnEntryIds,
    Guid? ReplayEntryId,
    string? ReplayFingerprint,
    StructureResolutionIndeterminacyCause? IndeterminacyCause = null,
    Guid? ActionMatchId = null,
    string? ActionMatchLabel = null,
    ResolutionSourceAbsence? SourceAbsence = null,
    Guid? ParsedSourceStageId = null);
