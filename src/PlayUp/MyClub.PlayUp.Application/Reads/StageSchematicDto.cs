// -----------------------------------------------------------------------
// <copyright file="StageSchematicDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Phase schematic read model — form units + placed entries (S1–S8).
/// No computed occupation flags; SPA derives empty/placed from <see cref="SchematicCaseDto.Entry"/>.
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="CompetitionId">Owning competition.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="FormatKind">Inferred structure format when known.</param>
/// <param name="Cases">Form units (possibly empty when form incomplete).</param>
/// <param name="Connections">Cup bracket wires only; empty for other formats.</param>
/// <param name="SwissRoundCount">Planned Swiss rounds (structural K) when Kind is Swiss.</param>
/// <param name="CupRoundCount">Cup rounds in this phase when Kind is Cup.</param>
/// <param name="GroupFeeds">Optional Groups A1 summary (compat); Structure SPA paints ForGroup WhoFeeds on empty seats in <see cref="Cases"/>.</param>
/// <param name="ExpectedFormParticipants">
/// Championship/Swiss expected form bag: Composition occupants + pending ForForm intentions.
/// Projected into <see cref="Cases"/> (N cells, non-addressing order).
/// </param>
public sealed record StageSchematicDto(
    Guid StageId,
    Guid CompetitionId,
    string Name,
    StageStatus Status,
    StructureFormatKind? FormatKind,
    IReadOnlyList<SchematicCaseDto> Cases,
    IReadOnlyList<SchematicConnectionDto> Connections,
    int? SwissRoundCount = null,
    int? CupRoundCount = null,
    IReadOnlyList<SchematicGroupFeedDto>? GroupFeeds = null,
    ExpectedFormParticipantsDto? ExpectedFormParticipants = null);

/// <summary>
/// Championship / Swiss — form participant bag for Structure (no RosterPlace addressing).
/// </summary>
/// <param name="Resolved">Entries actually present in Composition.</param>
/// <param name="Pending">ForForm paths without resolution provenance.</param>
public sealed record ExpectedFormParticipantsDto(
    IReadOnlyList<ExpectedResolvedFormParticipantDto> Resolved,
    IReadOnlyList<SchematicFeedOriginDto> Pending);

/// <summary>
/// One resolved form occupant (Composition entry).
/// </summary>
public sealed record ExpectedResolvedFormParticipantDto(
    SchematicEntryRefDto Entry,
    SchematicParticipantRefDto? Assignment);

/// <summary>
/// Groups A1 — feed origin at Groupe grain (never bound to Place k / GroupPlace index).
/// </summary>
/// <param name="GroupId">Destination group identity.</param>
/// <param name="FeedOrigin">Configured inbound Qual/Prog feed when Unique-like.</param>
public sealed record SchematicGroupFeedDto(
    Guid GroupId,
    SchematicFeedOriginDto FeedOrigin);

/// <summary>
/// One form unit and its optional feed / placement / resolution.
/// </summary>
/// <param name="FormPosition">Where the case exists in the form.</param>
/// <param name="FeedOrigin">Unique WhoFeeds (Cup slot / Groups) when known.</param>
/// <param name="Entry">Entry actually placed in this unit; null = empty.</param>
/// <param name="Assignment">Resolved participant of that entry when known.</param>
public sealed record SchematicCaseDto(
    SchematicFormPositionDto FormPosition,
    SchematicFeedOriginDto? FeedOrigin,
    SchematicEntryRefDto? Entry,
    SchematicParticipantRefDto? Assignment);

/// <summary>
/// Discriminated form position (<c>Kind</c>: CupSlot | GroupPlace | RosterPlace).
/// </summary>
/// <param name="Kind">Position kind.</param>
/// <param name="SlotKey">Cup slot key when the unit is a bound slot.</param>
/// <param name="GroupId">Group identity when Kind is GroupPlace.</param>
/// <param name="GroupName">Group display name when Kind is GroupPlace.</param>
/// <param name="Index">1-based place index (GroupPlace or RosterPlace).</param>
/// <param name="FixtureId">Backing fixture when the unit is bound to a cup confrontation.</param>
/// <param name="Side">Bracket side (<c>A</c> | <c>B</c>) when known for Cup address.</param>
/// <param name="RoundOrder">0-based Cup round order when the unit belongs to a fixture in that round.</param>
/// <param name="RoundName">Domain round display name (organizer-authored; not localized by Host).</param>
/// <param name="PairOrdinal">1-based pair ordinal within the round when the round has multiple fixtures; null for a single-pair round (e.g. Finale).</param>
public sealed record SchematicFormPositionDto(
    string Kind,
    string? SlotKey = null,
    Guid? GroupId = null,
    string? GroupName = null,
    int? Index = null,
    Guid? FixtureId = null,
    string? Side = null,
    int? RoundOrder = null,
    string? RoundName = null,
    int? PairOrdinal = null);

/// <summary>
/// Structured feed origin (no pre-baked display label).
/// </summary>
/// <param name="Kind">Domain feed kind (Qualification, Progression, Direct, Draw config).</param>
/// <param name="SourceStageId">Source stage when Qualif/Prog.</param>
/// <param name="SourceStageName">Source stage display name when Qualif/Prog (Structure chrome / tooltip).</param>
/// <param name="PathOrder">Qualification path order when Kind is Qualification.</param>
/// <param name="SelectionMode">Qualification selection mode.</param>
/// <param name="SelectionValue">Qualification selection value.</param>
/// <param name="SelectionEndValue">Qualification range end when applicable.</param>
/// <param name="RankingScope">Qualification ranking scope.</param>
/// <param name="GroupId">Source group when scope is Group.</param>
/// <param name="GroupName">Resolved source group name when known.</param>
/// <param name="AcrossGroupsPosition">Across-groups position when applicable.</param>
/// <param name="SourcePairKey">Progression structural source key (Cup = PairKey).</param>
/// <param name="SourceFixtureNumber">1-based index of the source fixture within its round when materialized (display overlay).</param>
/// <param name="Outcome">Progression outcome.</param>
/// <param name="DrawId">Draw feed target id when Kind is Draw.</param>
/// <param name="ConfiguredEntryId">Direct assignment configured entry.</param>
/// <param name="SlotKey">Destination slot key of this feed (Cup).</param>
/// <param name="DestinationGroupId">Destination group when this feed targets Groups Placement (A1).</param>
public sealed record SchematicFeedOriginDto(
    FeedKind Kind,
    Guid? SourceStageId = null,
    string? SourceStageName = null,
    int? PathOrder = null,
    SelectionMode? SelectionMode = null,
    int? SelectionValue = null,
    int? SelectionEndValue = null,
    RankingScope? RankingScope = null,
    Guid? GroupId = null,
    string? GroupName = null,
    int? AcrossGroupsPosition = null,
    string? SourcePairKey = null,
    int? SourceFixtureNumber = null,
    ProgressionOutcome? Outcome = null,
    Guid? DrawId = null,
    Guid? ConfiguredEntryId = null,
    string? SlotKey = null,
    Guid? DestinationGroupId = null);

/// <summary>
/// Placed entry identity.
/// </summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name when known.</param>
public sealed record SchematicEntryRefDto(Guid EntryId, string? DisplayName);

/// <summary>
/// Resolved participant presentation for a placed entry.
/// </summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name when known.</param>
/// <param name="ShortName">Optional short name.</param>
/// <param name="LogoMediaId">Optional logo media id.</param>
/// <param name="PrimaryColor">Optional primary color.</param>
/// <param name="SecondaryColor">Optional secondary color.</param>
public sealed record SchematicParticipantRefDto(
    Guid EntryId,
    string? DisplayName,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null);

/// <summary>
/// Cup bracket connection — structural <c>BracketPair</c> and optional materialized fixture.
/// </summary>
/// <param name="FixtureId">Fixture identity when materialized; null for structure-only pair.</param>
/// <param name="RoundOrder">0-based round index in the stage.</param>
/// <param name="SlotAKey">Slot A key.</param>
/// <param name="SlotBKey">Slot B key.</param>
/// <param name="MatchNumber">1-based Match # within the round when a fixture exists; 0 when structural only.</param>
/// <param name="PairKey">Persistent BracketPair identity (P1…); null for legacy unbound fixture wires.</param>
public sealed record SchematicConnectionDto(
    Guid? FixtureId,
    int RoundOrder,
    string? SlotAKey,
    string? SlotBKey,
    int MatchNumber,
    string? PairKey = null);
