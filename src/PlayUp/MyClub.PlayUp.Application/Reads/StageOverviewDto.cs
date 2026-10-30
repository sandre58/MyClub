// -----------------------------------------------------------------------
// <copyright file="StageOverviewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Product overview of a stage: structure, slots, fixtures, draws, and Cup BracketPairs.
/// </summary>
/// <param name="Id">Stage identity.</param>
/// <param name="CompetitionId">Owning competition.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="Rounds">Rounds with fixtures.</param>
/// <param name="Slots">Bracket / destination slots.</param>
/// <param name="Draws">Draw lifecycle and summarized resolution.</param>
/// <param name="BracketPairs">Cup structural confrontations (empty for Groups).</param>
/// <param name="Groups">Group membership snapshot (empty for non-Groups formats).</param>
/// <param name="DrawBracketLaunchFacts">Bracket occupancy for Tirage launch; null when draw mechanism not applicable.</param>
/// <param name="ReleaseCandidateDrawId">Most recent Cancelled draw with at least one aligned placement.</param>
/// <param name="SlotDrawDisplayMetrics">Current cup partial-draw counts. Null when the stage has no slots. Not a historical snapshot.</param>
public sealed record StageOverviewDto(
    Guid Id,
    Guid CompetitionId,
    string Name,
    StageStatus Status,
    IReadOnlyList<StageRoundDto> Rounds,
    IReadOnlyList<StageSlotDto> Slots,
    IReadOnlyList<StageDrawDto> Draws,
    IReadOnlyList<StageBracketPairDto> BracketPairs,
    IReadOnlyList<StageGroupOverviewDto> Groups,
    StageDrawBracketLaunchFactsDto? DrawBracketLaunchFacts = null,
    Guid? ReleaseCandidateDrawId = null,
    StageDrawDisplayMetricsDto? SlotDrawDisplayMetrics = null);

/// <summary>
/// Group membership line for stage overview (draw release / alignment checks).
/// </summary>
/// <param name="Id">Group identity.</param>
/// <param name="Name">Group display name.</param>
/// <param name="EntryIds">Entries currently assigned to the group.</param>
public sealed record StageGroupOverviewDto(
    Guid Id,
    string Name,
    IReadOnlyList<Guid> EntryIds);

/// <summary>
/// Structural Cup confrontation potential (PairKey P1… = SlotA ↔ SlotB).
/// </summary>
/// <param name="PairKey">Persistent pair identity (e.g. P1).</param>
/// <param name="SlotAKey">First place key.</param>
/// <param name="SlotBKey">Second place key.</param>
public sealed record StageBracketPairDto(string PairKey, string SlotAKey, string SlotBKey);

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
/// <param name="SlotPlacements">Slot placements when resolved as Slot; otherwise empty.</param>
/// <param name="GroupPlacements">Group placements when resolved as Group; otherwise empty.</param>
/// <param name="IsApplied">Derived occupancy (Publish ≠ Apply) via <c>DrawAppliedState</c>.</param>
/// <param name="IsObsoleteForConsumption">Draw resolution obsolete for Apply/Materialize consumption.</param>
/// <param name="Actions">Server projection of cancel, release, apply, publish-and-apply, and redo. Available is not a success guarantee.</param>
/// <param name="InputsIntent">Intent recorded on the draw. Derived display only; not a stored provenance of each team.</param>
/// <param name="DisplayMetrics">Draft slot metrics (vacant, unplaced, already placed). Null for group draws and for Published or Cancelled draws.</param>
/// <param name="SlotDisplayCells">Cup grid. Draft classifies the current phase. Published and Cancelled list only that draw's resolution, plus outside-draw for the other structural slots.</param>
public sealed record StageDrawDto(
    Guid Id,
    DrawResolutionKind Kind,
    DrawStatus Status,
    DrawResolutionState ResolutionState,
    IReadOnlyList<StageDrawSlotPlacementDto> SlotPlacements,
    IReadOnlyList<StageDrawGroupPlacementDto> GroupPlacements,
    bool IsApplied,
    bool IsObsoleteForConsumption,
    StageDrawActionsDto? Actions = null,
    DrawInputsIntent InputsIntent = DrawInputsIntent.Default,
    StageDrawDisplayMetricsDto? DisplayMetrics = null,
    IReadOnlyList<StageDrawSlotDisplayCellDto>? SlotDisplayCells = null);

/// <summary>
/// Slot placement result summary.
/// </summary>
/// <param name="SlotKey">Destination slot.</param>
/// <param name="EntryId">Placed entry.</param>
/// <param name="DisplayName">Display name when known.</param>
/// <param name="ShortName">Short name when known.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
/// <param name="Origin">Display origin of this resolution line for the draw being read. Resolution lines are <see cref="DrawPlacementOrigin.Draw"/>.</param>
public sealed record StageDrawSlotPlacementDto(
    string SlotKey,
    Guid EntryId,
    string? DisplayName,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    DrawPlacementOrigin? Origin = null);

/// <summary>
/// Read-time display origin. Not a stored identity of which draw produced the team.
/// </summary>
public enum DrawPlacementOrigin
{
    /// <summary>The team is a placement of the resolution of the draw being read.</summary>
    Draw = 0,

    /// <summary>The slot has a filled structural feed (qualification, progression, draw target, or a non-unique feed).</summary>
    Feed = 1,

    /// <summary>Manual placement, resolved occupant, or an occupant left by a previous draw. A unique direct assignment is manual.</summary>
    Manual = 2,
}

/// <summary>
/// Cup cell in the Tirage grid.
/// </summary>
public enum SlotDrawCellState
{
    /// <summary>In the resolution of the draw being read.</summary>
    Drawn = 0,

    /// <summary>Composition occupant preserved beside the draw. Draft only.</summary>
    AlreadyPlaced = 1,

    /// <summary>Filled structural feed. Not drawable. Draft only.</summary>
#pragma warning disable CA1700 // Product cell state name. Not an unused reserved slot.
    Reserved = 2,
#pragma warning restore CA1700

    /// <summary>Occupant outside composition. Not a free place. Draft only.</summary>
    Withdrawn = 3,

    /// <summary>Missing feed and no occupant. Draft only.</summary>
    Free = 4,

    /// <summary>Structural slot absent from this draw's resolution. Published and Cancelled only.</summary>
    OutsideDraw = 5,
}

/// <summary>
/// One cup cell. Team fields are null when the cell has no team to show.
/// </summary>
/// <param name="SlotKey">Slot key.</param>
/// <param name="CellState">How the cell renders.</param>
/// <param name="EntryId">Team when the cell shows one.</param>
/// <param name="DisplayName">Display name when known.</param>
/// <param name="ShortName">Short name when known.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color (#RRGGBB).</param>
/// <param name="Origin">Display origin when the cell is a placement or a preserved occupant. Null for a free or outside-draw cell.</param>
public sealed record StageDrawSlotDisplayCellDto(
    string SlotKey,
    SlotDrawCellState CellState,
    Guid? EntryId,
    string? DisplayName,
    string? ShortName,
    Guid? LogoMediaId,
    string? PrimaryColor,
    DrawPlacementOrigin? Origin);

/// <summary>
/// Partial-draw counts for the Tirage hero. Same classifier as <c>SlotPartialDraw</c>.
/// </summary>
/// <param name="UnplacedEntryCount">Composition entries that do not occupy a slot.</param>
/// <param name="VacantSlotKeyCount">Free slots (no occupant, feed missing). Withdrawn slots are excluded.</param>
/// <param name="VacantSlotKeys">Those free slot keys, in stage order.</param>
/// <param name="AlreadyPlacedEntryCount">Composition occupants without a structural feed.</param>
/// <param name="EntryCount">Composition size. Duo hero team count.</param>
/// <param name="PlaceCount">Slot count. Duo hero place count.</param>
public sealed record StageDrawDisplayMetricsDto(
    int UnplacedEntryCount,
    int VacantSlotKeyCount,
    IReadOnlyList<string> VacantSlotKeys,
    int AlreadyPlacedEntryCount,
    int EntryCount,
    int PlaceCount);

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

/// <summary>
/// Whether a projected draw action is offered.
/// </summary>
public enum StageDrawActionState
{
    /// <summary>The action is not offered.</summary>
    Hidden,

    /// <summary>The action is offered. Success is not guaranteed.</summary>
    Available,

    /// <summary>The action is offered and disabled. <c>blockCode</c> explains why.</summary>
    Blocked
}

/// <summary>
/// One projected draw action.
/// </summary>
/// <param name="State">Hidden, Available, or Blocked.</param>
/// <param name="BlockCode">Existing error code when <paramref name="State"/> is Blocked.</param>
public sealed record StageDrawActionDto(StageDrawActionState State, string? BlockCode = null);

/// <summary>
/// Projected Libérer action. <paramref name="ReleasableCount"/> excludes direct assignments.
/// </summary>
/// <param name="State">Hidden when the count is zero and nothing else blocks the command.</param>
/// <param name="BlockCode">Existing error code when <paramref name="State"/> is Blocked.</param>
/// <param name="ReleasableCount">Aligned placements the command would release.</param>
public sealed record StageDrawReleaseActionDto(
    StageDrawActionState State,
    string? BlockCode,
    int ReleasableCount);

/// <summary>
/// Draw actions on <c>GET /stages/{id}</c>.
/// </summary>
/// <param name="Cancel">Annuler.</param>
/// <param name="Release">Libérer.</param>
/// <param name="Apply">Appliquer a published draw.</param>
/// <param name="PublishAndApply">Publier et appliquer. Apply preconditions are evaluated while the draw is still Draft.</param>
/// <param name="Redo">Refaire. Not a success guarantee; the client still orchestrates cancel, release, then preview.</param>
public sealed record StageDrawActionsDto(
    StageDrawActionDto Cancel,
    StageDrawReleaseActionDto Release,
    StageDrawActionDto Apply,
    StageDrawActionDto PublishAndApply,
    StageDrawActionDto Redo);
