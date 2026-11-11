// -----------------------------------------------------------------------
// <copyright file="ResolutionReleaseResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages.ReleaseResolution;

/// <summary>
/// Preview or confirmation of one release.
/// </summary>
/// <param name="VersionToken">State token. Send it back to confirm.</param>
/// <param name="Releases">The place that is cleared.</param>
/// <param name="DetachedMatches">Unplayed matches whose side becomes to be determined.</param>
/// <param name="AffectedOccupants">Other places whose freshness changes.</param>
public sealed record ResolutionReleaseResult(
    string VersionToken,
    IReadOnlyList<ResolutionReleaseRow> Releases,
    IReadOnlyList<ResolutionReleaseDetachedMatch> DetachedMatches,
    IReadOnlyList<ResolutionReleaseAffectedOccupant> AffectedOccupants);

/// <summary>
/// One place cleared by the release.
/// </summary>
public sealed record ResolutionReleaseRow(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId);

/// <summary>
/// One unplayed match whose side is cleared.
/// </summary>
public sealed record ResolutionReleaseDetachedMatch(
    Guid MatchId,
    Guid StageId,
    int LegIndex,
    Side Side,
    Guid? HomeEntryId,
    Guid? AwayEntryId);

/// <summary>
/// Another place whose freshness changes when this place is released.
/// </summary>
public sealed record ResolutionReleaseAffectedOccupant(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId,
    StructureResolutionFreshness StateBefore,
    StructureResolutionFreshness StateAfter);

/// <summary>
/// The place the organiser asked to release.
/// </summary>
/// <param name="Target">Slot, group, or population.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
public sealed record ResolutionReleaseTarget(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);
