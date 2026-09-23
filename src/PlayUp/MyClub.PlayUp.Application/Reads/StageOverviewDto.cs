// -----------------------------------------------------------------------
// <copyright file="StageOverviewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Product overview of a stage: structure, slots, fixtures, and draws.
/// </summary>
/// <param name="Id">Stage identity.</param>
/// <param name="CompetitionId">Owning competition.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="Rounds">Rounds with fixtures.</param>
/// <param name="Slots">Bracket / destination slots.</param>
/// <param name="Draws">Draw lifecycle and summarized resolution.</param>
public sealed record StageOverviewDto(
    Guid Id,
    Guid CompetitionId,
    string Name,
    StageStatus Status,
    IReadOnlyList<StageRoundDto> Rounds,
    IReadOnlyList<StageSlotDto> Slots,
    IReadOnlyList<StageDrawDto> Draws);

/// <summary>
/// Round with fixtures for stage overview.
/// </summary>
/// <param name="Id">Round identity.</param>
/// <param name="Name">Round display name.</param>
/// <param name="Fixtures">Fixtures in this round.</param>
public sealed record StageRoundDto(Guid Id, string Name, IReadOnlyList<StageFixtureDto> Fixtures);

/// <summary>
/// Fixture line for stage overview.
/// </summary>
/// <param name="Id">Fixture identity.</param>
/// <param name="SlotAKey">Optional bracket slot A.</param>
/// <param name="SlotBKey">Optional bracket slot B.</param>
/// <param name="Attachments">Attached matches (leg index).</param>
public sealed record StageFixtureDto(
    Guid Id,
    string? SlotAKey,
    string? SlotBKey,
    IReadOnlyList<StageFixtureAttachmentDto> Attachments);

/// <summary>
/// Match attachment on a fixture.
/// </summary>
/// <param name="MatchId">Attached match.</param>
/// <param name="LegIndex">Leg index within the fixture.</param>
public sealed record StageFixtureAttachmentDto(Guid MatchId, int LegIndex);

/// <summary>
/// Slot line for stage overview (progression visible via <see cref="EntryId"/>).
/// </summary>
/// <param name="SlotKey">Business slot key.</param>
/// <param name="EntryId">Resolved occupant when set.</param>
/// <param name="DisplayName">Occupant display name when known.</param>
/// <param name="CoveredByCompleteFixture">True when a complete SlotA/B Fixture already covers this key (from-slots pairing excludes it).</param>
public sealed record StageSlotDto(
    string SlotKey,
    Guid? EntryId,
    string? DisplayName,
    bool CoveredByCompleteFixture);

/// <summary>
/// Draw summary for stage overview (no rules, inputs, or soft violations).
/// </summary>
/// <param name="Id">Draw identity.</param>
/// <param name="Kind">Resolution kind.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="ResolutionState">Resolution state.</param>
/// <param name="Pairings">Pairing results when resolved as Pairing; otherwise empty.</param>
/// <param name="SlotPlacements">Slot placements when resolved as Slot; otherwise empty.</param>
/// <param name="GroupPlacements">Group placements when resolved as Group; otherwise empty.</param>
/// <param name="IsApplied">Derived occupancy (Publish ≠ Apply) via <c>DrawAppliedState</c>.</param>
public sealed record StageDrawDto(
    Guid Id,
    DrawResolutionKind Kind,
    DrawStatus Status,
    DrawResolutionState ResolutionState,
    IReadOnlyList<StageDrawPairingDto> Pairings,
    IReadOnlyList<StageDrawSlotPlacementDto> SlotPlacements,
    IReadOnlyList<StageDrawGroupPlacementDto> GroupPlacements,
    bool IsApplied);

/// <summary>
/// Pairing result summary.
/// </summary>
/// <param name="EntryAId">First pairing entry.</param>
/// <param name="EntryADisplayName">Display name of entry A when known.</param>
/// <param name="EntryAShortName">Short name of entry A when known.</param>
/// <param name="EntryBId">Second pairing entry.</param>
/// <param name="EntryBDisplayName">Display name of entry B when known.</param>
/// <param name="EntryBShortName">Short name of entry B when known.</param>
/// <param name="EntryALogoMediaId">Optional Media Guid for entry A logo.</param>
/// <param name="EntryAPrimaryColor">Optional primary kit color for entry A.</param>
/// <param name="EntryBLogoMediaId">Optional Media Guid for entry B logo.</param>
/// <param name="EntryBPrimaryColor">Optional primary kit color for entry B.</param>
public sealed record StageDrawPairingDto(
    Guid EntryAId,
    string? EntryADisplayName,
    string? EntryAShortName,
    Guid EntryBId,
    string? EntryBDisplayName,
    string? EntryBShortName,
    Guid? EntryALogoMediaId = null,
    string? EntryAPrimaryColor = null,
    Guid? EntryBLogoMediaId = null,
    string? EntryBPrimaryColor = null);

/// <summary>
/// Slot placement result summary.
/// </summary>
/// <param name="SlotKey">Destination slot.</param>
/// <param name="EntryId">Placed entry.</param>
/// <param name="DisplayName">Display name when known.</param>
/// <param name="ShortName">Short name when known.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
public sealed record StageDrawSlotPlacementDto(
    string SlotKey,
    Guid EntryId,
    string? DisplayName,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null);

/// <summary>
/// Group placement result summary.
/// </summary>
/// <param name="GroupId">Destination group.</param>
/// <param name="GroupDisplayName">Group display name when known.</param>
/// <param name="EntryId">Placed entry.</param>
/// <param name="DisplayName">Entry display name when known.</param>
/// <param name="ShortName">Short name when known.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
public sealed record StageDrawGroupPlacementDto(
    Guid GroupId,
    string? GroupDisplayName,
    Guid EntryId,
    string? DisplayName,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null);
