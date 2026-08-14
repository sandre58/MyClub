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

        var rounds = stage.Rounds
            .Select(round => new StageRoundDto(
                round.Id.Value,
                round.Name,
                [.. round.Fixtures.Select(MapFixture)]))
            .ToArray();

        var slots = stage.Slots
            .Select(slot => new StageSlotDto(
                slot.SlotKey,
                slot.EntryId?.Value,
                slot.EntryId is { } entryId ? EntryDisplayNames.Resolve(names, entryId) : null))
            .ToArray();

        var draws = stage.Draws.Select(draw => MapDraw(draw, names)).ToArray();

        return new StageOverviewDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            rounds,
            slots,
            draws);
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

    private static StageDrawDto MapDraw(Draw draw, IReadOnlyDictionary<EntryId, string> names)
    {
        var resolution = draw.Resolution;
        IReadOnlyList<StageDrawPairingDto> pairings = [];
        IReadOnlyList<StageDrawSlotPlacementDto> slotPlacements = [];

        if (resolution.State != DrawResolutionState.Resolved)
        {
            return new StageDrawDto(
                draw.Id.Value,
                draw.Kind,
                draw.Status,
                resolution.State,
                pairings,
                slotPlacements);
        }

        pairings =
        [
            .. resolution.PairingResults
                .Select(pairing => new StageDrawPairingDto(
                    pairing.EntryA.Value,
                    EntryDisplayNames.Resolve(names, pairing.EntryA),
                    pairing.EntryB.Value,
                    EntryDisplayNames.Resolve(names, pairing.EntryB)))
        ];

        slotPlacements =
        [
            .. resolution.SlotResults
                .Select(placement => new StageDrawSlotPlacementDto(
                    placement.SlotKey,
                    placement.EntryId.Value,
                    EntryDisplayNames.Resolve(names, placement.EntryId)))
        ];

        return new StageDrawDto(
            draw.Id.Value,
            draw.Kind,
            draw.Status,
            resolution.State,
            pairings,
            slotPlacements);
    }
}
