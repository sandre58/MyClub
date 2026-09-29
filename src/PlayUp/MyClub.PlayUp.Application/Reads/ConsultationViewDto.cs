// -----------------------------------------------------------------------
// <copyright file="ConsultationViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Product Consultation destination — derived read, never persisted.
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="CompletionMode">How the competition was completed, when set.</param>
/// <param name="FormatKind">Inferred primary format, when structured.</param>
/// <param name="FormatLabel">Human format label.</param>
/// <param name="Results">Played / cancelled match results (MatchId for Match Detail deep-link).</param>
/// <param name="Standings">Derived standings section (may be not applicable).</param>
/// <param name="Structure">Sporting structure view.</param>
public sealed record ConsultationViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    CompletionMode? CompletionMode,
    StructureFormatKind? FormatKind,
    string FormatLabel,
    IReadOnlyList<ConsultationResultDto> Results,
    ConsultationStandingsSectionDto Standings,
    ConsultationStructureDto Structure);

/// <summary>
/// One result line for Consultation Results.
/// </summary>
public sealed record ConsultationResultDto(
    Guid MatchId,
    Guid StageId,
    Guid? FixtureId,
    Guid? RoundId,
    int? MatchdayNumber,
    string? ContextLabel,
    MatchStatus Status,
    EntrySideDto Home,
    EntrySideDto Away,
    MatchScoreDto? Score,
    ResultType? ResultType,
    DateTimeOffset? ScheduledAt);

/// <summary>
/// Standings section — derived tables or not-applicable diagnostic.
/// </summary>
/// <param name="Applicable">False for Cup / empty structure.</param>
/// <param name="NotApplicableReason">Machine reason when not applicable.</param>
/// <param name="Tables">Standing tables (overall and/or groups).</param>
public sealed record ConsultationStandingsSectionDto(
    bool Applicable,
    string? NotApplicableReason,
    IReadOnlyList<ConsultationStandingTableDto> Tables);

/// <summary>
/// One standing table (overall or a group).
/// </summary>
public sealed record ConsultationStandingTableDto(
    string Scope,
    Guid StageId,
    string StageName,
    Guid? GroupId,
    string? GroupName,
    IReadOnlyList<ConsultationStandingRowDto> Rows);

/// <summary>
/// Standing row for product display.
/// </summary>
public sealed record ConsultationStandingRowDto(
    int Position,
    Guid EntryId,
    string DisplayName,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);

/// <summary>
/// Structure section for Consultation.
/// </summary>
public sealed record ConsultationStructureDto(
    StructureFormatKind? FormatKind,
    IReadOnlyList<ConsultationStageStructureDto> Stages);

/// <summary>
/// Stage structure for Consultation (groups / matchdays / rounds / slots).
/// </summary>
public sealed record ConsultationStageStructureDto(
    Guid StageId,
    string Name,
    StageStatus Status,
    StructureFormatKind? FormatKind,
    MatchGenerationFormat MatchGenerationFormat,
    IReadOnlyList<ConsultationGroupStructureDto> Groups,
    IReadOnlyList<ConsultationMatchdayStructureDto> Matchdays,
    IReadOnlyList<ConsultationRoundStructureDto> Rounds,
    IReadOnlyList<ConsultationSlotStructureDto> Slots);

/// <summary>Group line with occupants.</summary>
public sealed record ConsultationGroupStructureDto(
    Guid GroupId,
    string Name,
    IReadOnlyList<EntrySideDto> Entries);

/// <summary>Matchday with fixture/match refs.</summary>
public sealed record ConsultationMatchdayStructureDto(
    Guid MatchdayId,
    int Number,
    IReadOnlyList<ConsultationFixtureStructureDto> Fixtures);

/// <summary>Round with fixture/match refs.</summary>
public sealed record ConsultationRoundStructureDto(
    Guid RoundId,
    string Name,
    IReadOnlyList<ConsultationFixtureStructureDto> Fixtures);

/// <summary>Fixture with attached match ids.</summary>
public sealed record ConsultationFixtureStructureDto(
    Guid FixtureId,
    string? SlotAKey,
    string? SlotBKey,
    IReadOnlyList<ConsultationFixtureMatchRefDto> Matches);

/// <summary>Match attachment on a fixture.</summary>
public sealed record ConsultationFixtureMatchRefDto(Guid MatchId, int LegIndex);

/// <summary>Slot occupancy for cup/bracket structure.</summary>
public sealed record ConsultationSlotStructureDto(string SlotKey, Guid? EntryId, string? DisplayName);
