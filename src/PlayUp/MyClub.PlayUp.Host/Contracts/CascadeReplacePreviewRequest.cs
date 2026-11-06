// -----------------------------------------------------------------------
// <copyright file="CascadeReplacePreviewRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Body of <c>POST /stages/{stageId}/resolution/cascade-preview</c>.
/// </summary>
/// <param name="Target">Occupant surface.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Slot"/>.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is <see cref="StructureResolutionOccupantTarget.Group"/>.</param>
public sealed record CascadeReplacePreviewRequest(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);

/// <summary>
/// Body of <c>POST /stages/{stageId}/resolution/cascade-replace</c>.
/// </summary>
/// <param name="Target">Occupant surface.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
/// <param name="VersionToken">Token returned by the preview.</param>
public sealed record CascadeReplaceConfirmRequest(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId,
    string VersionToken);

/// <summary>
/// Preview or confirmation payload.
/// </summary>
/// <param name="VersionToken">State token. Send it back to confirm.</param>
/// <param name="PlayedMatches">Finished matches the cascade returns to not played.</param>
/// <param name="Replacements">Occupants whose entry changes. Empty on a preview that only warns.</param>
public sealed record CascadeReplaceResponse(
    string VersionToken,
    IReadOnlyList<CascadePlayedMatchDto> PlayedMatches,
    IReadOnlyList<CascadeReplacementDto> Replacements);

/// <summary>
/// One finished match listed in the cascade warning.
/// </summary>
public sealed record CascadePlayedMatchDto(
    Guid MatchId,
    Guid StageId,
    Guid? HomeEntryId,
    Guid? AwayEntryId);

/// <summary>
/// One occupant the cascade rewrote.
/// </summary>
public sealed record CascadeReplacementDto(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid PreviousEntryId,
    Guid EntryId);
