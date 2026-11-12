// -----------------------------------------------------------------------
// <copyright file="DefinitiveResolutionReleasePreviewRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Body of <c>POST /stages/{sourceId}/resolution/release-definitive-preview</c>.
/// </summary>
/// <param name="DestinationStageId">When set, only places stored on this phase are considered.</param>
public sealed record DefinitiveResolutionReleasePreviewRequest(Guid? DestinationStageId);

/// <summary>
/// Body of <c>POST /stages/{sourceId}/resolution/release-definitive</c>.
/// </summary>
/// <param name="DestinationStageId">Destination filter used by the preview. Null considers every destination.</param>
/// <param name="VersionToken">Token returned by the preview.</param>
/// <param name="Places">Primary places from the preview, in that order. Population side effects are omitted.</param>
public sealed record DefinitiveResolutionReleaseConfirmRequest(
    Guid? DestinationStageId,
    string VersionToken,
    IReadOnlyList<DefinitiveResolutionReleasePlaceRequest> Places);

/// <summary>
/// One primary place to confirm.
/// </summary>
public sealed record DefinitiveResolutionReleasePlaceRequest(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);

/// <summary>
/// Grouped preview or confirmation payload.
/// </summary>
public sealed record DefinitiveResolutionReleaseResponse(
    string VersionToken,
    IReadOnlyList<DefinitiveResolutionReleaseRowDto> Releases,
    IReadOnlyList<ResolutionReleaseDetachedMatchDto> DetachedMatches,
    IReadOnlyList<DefinitiveResolutionReleaseExclusionDto> Exclusions,
    IReadOnlyList<ResolutionReleaseAffectedOccupantDto> AffectedOccupants);

/// <summary>
/// One place cleared by the grouped release, or the population row removed with it.
/// </summary>
public sealed record DefinitiveResolutionReleaseRowDto(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId,
    string? EntryName,
    string? StageName,
    string? PlaceLabel,
    bool PopulationRemoved,
    bool SideEffect);

/// <summary>
/// A definitive place left in place, with the refusal or exception code.
/// </summary>
public sealed record DefinitiveResolutionReleaseExclusionDto(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId,
    string? EntryName,
    string? StageName,
    string? PlaceLabel,
    string Code);
