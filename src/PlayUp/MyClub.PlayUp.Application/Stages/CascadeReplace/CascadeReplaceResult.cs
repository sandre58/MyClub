// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Application.Stages.CascadeReplace;

/// <summary>
/// Outcome of a cascade preview or confirmation. The version token is the state before mutation.
/// </summary>
/// <param name="VersionToken">Token the confirmation must send back.</param>
/// <param name="PlayedMatches">Finished downstream matches whose result will be or was dropped.</param>
/// <param name="Replacements">Occupants whose entry changed.</param>
public sealed record CascadeReplaceResult(
    string VersionToken,
    IReadOnlyList<CascadePlayedMatch> PlayedMatches,
    IReadOnlyList<CascadeReplacement> Replacements);

/// <summary>
/// A finished match the cascade returns to not played.
/// Names and the leg are filled from the ids captured before the participants change.
/// </summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="StageId">Stage that owns the match.</param>
/// <param name="HomeEntryId">Home entry before the cascade.</param>
/// <param name="AwayEntryId">Away entry before the cascade.</param>
/// <param name="HomeName">Display name of <paramref name="HomeEntryId"/>, or null when missing.</param>
/// <param name="AwayName">Display name of <paramref name="AwayEntryId"/>, or null when missing.</param>
/// <param name="StageName">Name of the stage that owns the match, or null when missing.</param>
/// <param name="LegIndex">Leg of a multi-leg tie, or null for a single leg or an unknown attachment.</param>
public sealed record CascadePlayedMatch(
    Guid MatchId,
    Guid StageId,
    Guid? HomeEntryId,
    Guid? AwayEntryId,
    string? HomeName = null,
    string? AwayName = null,
    string? StageName = null,
    int? LegIndex = null);

/// <summary>
/// One occupant whose stored entry changed.
/// </summary>
/// <param name="StageId">Destination stage.</param>
/// <param name="Target">Surface that changed.</param>
/// <param name="SlotKey">Slot key when the surface is a slot.</param>
/// <param name="GroupId">Group id when the surface is a group.</param>
/// <param name="PreviousEntryId">Entry that left.</param>
/// <param name="EntryId">Entry that arrived.</param>
public sealed record CascadeReplacement(
    Guid StageId,
    StructureResolutionOccupantTarget Target,
    string? SlotKey,
    Guid? GroupId,
    Guid PreviousEntryId,
    Guid EntryId);

/// <summary>
/// The stale occupant the organiser confirms.
/// </summary>
/// <param name="Target">Surface the organiser pointed at.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
public sealed record CascadeReplaceTarget(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    string? SlotKey,
    Guid? GroupId);
