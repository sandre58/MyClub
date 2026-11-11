// -----------------------------------------------------------------------
// <copyright file="ResolutionReleasePreviewRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Body of <c>POST /stages/{stageId}/resolution/release-preview</c>.
/// </summary>
/// <param name="Target">Occupant surface.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Slot"/>.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Group"/>.</param>
public sealed record ResolutionReleasePreviewRequest(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);

/// <summary>
/// Body of <c>POST /stages/{stageId}/resolution/release</c>.
/// </summary>
/// <param name="Target">Occupant surface.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
/// <param name="VersionToken">Token returned by the preview.</param>
public sealed record ResolutionReleaseConfirmRequest(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId,
    string VersionToken);

/// <summary>
/// Preview or confirmation payload.
/// </summary>
/// <param name="VersionToken">State token. Send it back to confirm.</param>
/// <param name="Releases">The place that is cleared.</param>
/// <param name="DetachedMatches">Unplayed matches whose side becomes to be determined.</param>
/// <param name="AffectedOccupants">Other places whose freshness changes.</param>
public sealed record ResolutionReleaseResponse(
    string VersionToken,
    IReadOnlyList<ResolutionReleaseRowDto> Releases,
    IReadOnlyList<ResolutionReleaseDetachedMatchDto> DetachedMatches,
    IReadOnlyList<ResolutionReleaseAffectedOccupantDto> AffectedOccupants);

/// <summary>
/// One place cleared by the release.
/// </summary>
public sealed record ResolutionReleaseRowDto(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId);

/// <summary>
/// One unplayed match whose side is cleared.
/// </summary>
public sealed record ResolutionReleaseDetachedMatchDto(
    Guid MatchId,
    Guid StageId,
    int LegIndex,
    Side Side,
    Guid? HomeEntryId,
    Guid? AwayEntryId);

/// <summary>
/// Another place whose freshness changes when this place is released.
/// </summary>
public sealed record ResolutionReleaseAffectedOccupantDto(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId,
    StructureResolutionFreshness StateBefore,
    StructureResolutionFreshness StateAfter);
