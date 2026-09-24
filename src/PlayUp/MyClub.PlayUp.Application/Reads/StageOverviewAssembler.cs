// -----------------------------------------------------------------------
// <copyright file="StageOverviewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="StageOverviewDto"/> from Stage + Competition (display names).
/// </summary>
public static class StageOverviewAssembler
{
    /// <summary>
    /// Maps stage structure, slots, and draws into a product overview (no Domain mutation).
    /// </summary>
    /// <param name="stage">Loaded stage (with relations).</param>
    /// <param name="competition">Owning competition for entry display names.</param>
    /// <returns>The assembled overview.</returns>
    public static StageOverviewDto Assemble(Stage stage, Competition competition)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        var names = EntryDisplayNames.ToMap(competition);
        var entries = EntryDisplayNames.ToEntries(competition);

        var rounds = stage.Rounds
            .Select(round => new StageRoundDto(
                round.Id.Value,
                round.Name,
                [.. round.Fixtures.Select(MapFixture)]))
            .ToArray();

        var covered = CupSlotCoverage.GetSlotsCoveredByCompleteFixtures(stage);

        var slots = stage.Slots
            .Select(slot => new StageSlotDto(
                slot.SlotKey,
                slot.EntryId?.Value,
                slot.EntryId is { } entryId ? EntryDisplayNames.Resolve(names, entryId) : null,
                covered.Contains(slot.SlotKey)))
            .ToArray();

        var draws = stage.Draws.Select(draw => MapDraw(draw, stage, entries)).ToArray();

        var bracketPairs = stage.BracketPairs
            .Select(pair => new StageBracketPairDto(pair.PairKey, pair.SlotAKey, pair.SlotBKey))
            .ToArray();

        return new StageOverviewDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            rounds,
            slots,
            draws,
            bracketPairs);
    }

    private static StageFixtureDto MapFixture(Fixture fixture) =>
        new(
            fixture.Id.Value,
            fixture.SlotAKey,
            fixture.SlotBKey,
            [
                .. fixture.Attachments
                    .OrderBy(attachment => attachment.LegIndex)
                    .Select(attachment => new StageFixtureAttachmentDto(attachment.MatchId.Value, attachment.LegIndex))
            ]);

    private static StageDrawDto MapDraw(
        Draw draw,
        Stage stage,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var resolution = draw.Resolution;
        IReadOnlyList<StageDrawSlotPlacementDto> slotPlacements = [];
        IReadOnlyList<StageDrawGroupPlacementDto> groupPlacements = [];

        if (resolution.State != DrawResolutionState.Resolved)
        {
            return new StageDrawDto(
                draw.Id.Value,
                draw.Kind,
                draw.Status,
                resolution.State,
                slotPlacements,
                groupPlacements,
                IsApplied: false);
        }

        slotPlacements =
        [
            .. resolution.SlotResults
                .Select(placement =>
                {
                    var side = EntryDisplayNames.ToSide(entries, placement.EntryId);
                    return new StageDrawSlotPlacementDto(
                        placement.SlotKey,
                        placement.EntryId.Value,
                        side.DisplayName,
                        side.ShortName,
                        side.LogoMediaId,
                        side.PrimaryColor);
                })
        ];

        var groupNames = stage.Groups.ToDictionary(
            group => group.Id,
            group => group.Name);

        groupPlacements =
        [
            .. resolution.GroupResults
                .Select(placement =>
                {
                    var side = EntryDisplayNames.ToSide(entries, placement.EntryId);
                    return new StageDrawGroupPlacementDto(
                        placement.GroupId.Value,
                        groupNames.GetValueOrDefault(placement.GroupId),
                        placement.EntryId.Value,
                        side.DisplayName,
                        side.ShortName,
                        side.LogoMediaId,
                        side.PrimaryColor);
                })
        ];

        return new StageDrawDto(
            draw.Id.Value,
            draw.Kind,
            draw.Status,
            resolution.State,
            slotPlacements,
            groupPlacements,
            DrawAppliedState.IsApplied(draw, stage));
    }
}
