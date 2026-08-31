// -----------------------------------------------------------------------
// <copyright file="OrganisationViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembled Organisation hub read for Slice 2 (not a Domain mirror).
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Competition name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Participants">Participant summary.</param>
/// <param name="Format">Inferred / configured format summary.</param>
/// <param name="Regulation">High-level regulation summary.</param>
/// <param name="Structure">Structure counts (not full Domain graph).</param>
/// <param name="Actions">Available organisation action codes.</param>
/// <param name="Readiness">Application readiness diagnostic for Slice 3.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="ScheduledStart">Optional declared start.</param>
/// <param name="ScheduledEnd">Optional declared end.</param>
public sealed record OrganisationViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    OrganisationParticipantsSummaryDto Participants,
    OrganisationFormatSummaryDto Format,
    OrganisationRegulationSummaryDto Regulation,
    OrganisationStructureSummaryDto Structure,
    IReadOnlyList<string> Actions,
    OrganisationReadinessDto Readiness,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    DateTimeOffset? ScheduledStart = null,
    DateTimeOffset? ScheduledEnd = null);

/// <summary>Participants section.</summary>
/// <param name="ActiveCount">Active entries.</param>
/// <param name="OccupyingCount">Occupying entries (Active/Qualified/Eliminated).</param>
/// <param name="Entries">Light entry rows.</param>
public sealed record OrganisationParticipantsSummaryDto(
    int ActiveCount,
    int OccupyingCount,
    IReadOnlyList<OrganisationEntryDto> Entries);

/// <summary>Entry row for Organisation.</summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Status">Entry status.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color.</param>
/// <param name="SecondaryColor">Optional secondary kit color.</param>
/// <param name="DeclaredMembers">Declared roster members for this entry.</param>
public sealed record OrganisationEntryDto(
    Guid EntryId,
    string DisplayName,
    EntryStatus Status,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null,
    IReadOnlyList<DeclaredMemberDto>? DeclaredMembers = null);

/// <summary>Format summary (Application inference — not Domain Format aggregate).</summary>
/// <param name="Kind">Inferred format, or null when structure empty (authoritative for SPA i18n).</param>
/// <param name="PrimaryStageId">Primary stage id when present.</param>
/// <param name="PrimaryStageName">Primary stage name when present.</param>
/// <param name="PrimaryStageStatus">Primary stage status when present.</param>
public sealed record OrganisationFormatSummaryDto(
    StructureFormatKind? Kind,
    Guid? PrimaryStageId,
    string? PrimaryStageName,
    StageStatus? PrimaryStageStatus);

/// <summary>Regulation summary.</summary>
/// <param name="MinimumTeams">EntryRules.MinimumTeams.</param>
/// <param name="MaximumTeams">EntryRules.MaximumTeams.</param>
/// <param name="DurationPerPeriod">Match duration per period.</param>
/// <param name="NumberOfPeriods">Match periods.</param>
/// <param name="WinPoints">Standing win points.</param>
/// <param name="DrawPoints">Standing draw points.</param>
/// <param name="LossPoints">Standing loss points.</param>
/// <param name="AllowedTypes">Authorized disciplinary catalogue types (empty = none).</param>
public sealed record OrganisationRegulationSummaryDto(
    int MinimumTeams,
    int MaximumTeams,
    int DurationPerPeriod,
    int NumberOfPeriods,
    int WinPoints,
    int DrawPoints,
    int LossPoints,
    IReadOnlyList<DisciplinaryType> AllowedTypes);

/// <summary>Structure counts for the primary stage.</summary>
/// <param name="GroupCount">Groups.</param>
/// <param name="RoundCount">Rounds.</param>
/// <param name="MatchdayCount">Matchdays.</param>
/// <param name="SlotCount">Slots.</param>
/// <param name="HasDrawRules">Whether stage DrawRules are set.</param>
/// <param name="NumberOfPots">PotRules.NumberOfPots when present.</param>
/// <param name="MatchGenerationFormat">Championship / Groups generation mode.</param>
/// <param name="SwissRoundCount">Planned Swiss rounds K when Kind is Swiss.</param>
public sealed record OrganisationStructureSummaryDto(
    int GroupCount,
    int RoundCount,
    int MatchdayCount,
    int SlotCount,
    bool HasDrawRules,
    int? NumberOfPots,
    MatchGenerationFormat MatchGenerationFormat,
    int? SwissRoundCount = null);

/// <summary>Application readiness diagnostic (not persisted, not Domain).</summary>
/// <param name="ReadyForNextSlice">True when organisation is sufficient for Slice 3 entry.</param>
/// <param name="ReadyForDraw">True when a Draw path is identifiable.</param>
/// <param name="ReadyForMaterialization">True when Fixtures/Matches can be materialized (Cup: primary skeleton fixtures incomplete; not from-slots).</param>
/// <param name="ReadyForSchedule">True when attached Matches exist for scheduling.</param>
/// <param name="ReadyForMatchOperation">True when Slice 4 can start (Matches attached; schedule optional).</param>
/// <param name="ReadyForSchedulePath">Legacy Slice 2 hint: championship schedule path identifiable from structure.</param>
/// <param name="AttachedMatchCount">Matches attached to the primary stage.</param>
/// <param name="Blockers">Machine-readable blocker codes (authoritative for SPA i18n).</param>
public sealed record OrganisationReadinessDto(
    bool ReadyForNextSlice,
    bool ReadyForDraw,
    bool ReadyForMaterialization,
    bool ReadyForSchedule,
    bool ReadyForMatchOperation,
    bool ReadyForSchedulePath,
    int AttachedMatchCount,
    IReadOnlyList<string> Blockers);
