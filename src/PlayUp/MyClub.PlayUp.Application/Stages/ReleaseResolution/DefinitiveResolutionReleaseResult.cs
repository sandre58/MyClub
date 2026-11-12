// -----------------------------------------------------------------------
// <copyright file="DefinitiveResolutionReleaseResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Application.Stages.ReleaseResolution;

/// <summary>
/// Preview or confirmation of one grouped release of definitive places.
/// </summary>
/// <param name="VersionToken">State token. Send it back with <see cref="Releases"/> that are not side effects.</param>
/// <param name="Releases">Places cleared. A population row added because the team leaves the composition is a side effect.</param>
/// <param name="DetachedMatches">Unplayed matches whose side becomes to be determined.</param>
/// <param name="Exclusions">Definitive places the guards or the release itself refused. The rest of the batch continues.</param>
/// <param name="AffectedOccupants">Other places whose freshness changes after a full recalculation.</param>
public sealed record DefinitiveResolutionReleaseResult(
    string VersionToken,
    IReadOnlyList<DefinitiveResolutionReleaseRow> Releases,
    IReadOnlyList<ResolutionReleaseDetachedMatch> DetachedMatches,
    IReadOnlyList<DefinitiveResolutionReleaseExclusion> Exclusions,
    IReadOnlyList<ResolutionReleaseAffectedOccupant> AffectedOccupants);

/// <summary>
/// One place cleared by the grouped release, or the population row removed with it.
/// </summary>
/// <remarks>
/// <see cref="DefinitiveResolutionReleaseRow.PopulationRemoved"/> is true when this row is the place
/// whose release also removed the team from the composition, or the place itself is that population.
/// <see cref="DefinitiveResolutionReleaseRow.SideEffect"/> is true when this row is the population removed
/// because another place on the same phase was released. Confirmation does not send side effects back.
/// </remarks>
public sealed record DefinitiveResolutionReleaseRow(
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
public sealed record DefinitiveResolutionReleaseExclusion(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid EntryId,
    string? EntryName,
    string? StageName,
    string? PlaceLabel,
    string Code);

/// <summary>
/// One primary place the confirmation must name. Population side effects are not included.
/// </summary>
public sealed record DefinitiveResolutionReleasePlace(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);
