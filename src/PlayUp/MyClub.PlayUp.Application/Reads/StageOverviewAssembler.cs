// -----------------------------------------------------------------------
// <copyright file="StageOverviewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
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
    /// <param name="competitionStages">Competition stages for inbound feed classification (defaults to target only).</param>
    /// <param name="participantSnapshots">Participant rows for Group Draft release coherence. Empty when not needed.</param>
    /// <param name="hasAttachedMatches">When set, overrides skeleton attachment detection for action projection.</param>
    /// <returns>The assembled overview.</returns>
    public static StageOverviewDto Assemble(
        Stage stage,
        Competition competition,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyList<MatchParticipantSnapshot>? participantSnapshots = null,
        bool? hasAttachedMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        var stagesForFeeds = competitionStages ?? [stage];
        var actionContext = new DrawActionContext(
            stage,
            stagesForFeeds,
            participantSnapshots ?? [],
            hasAttachedMatches ?? StructureSkeleton.CountAttachedMatches(stage) > 0);

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

        var slotDrawDisplayMetrics = StageDrawDisplay.MetricsForStage(stage, stagesForFeeds);

        var draws = stage.Draws
            .Select(draw => MapDraw(
                draw,
                stage,
                entries,
                actionContext,
                competition.Status,
                slotDrawDisplayMetrics))
            .ToArray();

        var bracketPairs = stage.BracketPairs
            .Select(pair => new StageBracketPairDto(pair.PairKey, pair.SlotAKey, pair.SlotBKey))
            .ToArray();

        var groups = stage.Groups
            .Select(group => new StageGroupOverviewDto(
                group.Id.Value,
                group.Name,
                [.. group.EntryIds.Select(entryId => entryId.Value)]))
            .ToArray();

        var drawBracketLaunchFacts =
            StageDrawBracketLaunchClassifier.TryClassify(stage, stagesForFeeds);

        return new StageOverviewDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            rounds,
            slots,
            draws,
            bracketPairs,
            groups,
            drawBracketLaunchFacts,
            DrawActionEvaluator.ReleaseCandidateDrawId(actionContext, stage),
            slotDrawDisplayMetrics);
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
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        DrawActionContext actionContext,
        CompetitionStatus competitionStatus,
        StageDrawDisplayMetricsDto? slotDrawDisplayMetrics)
    {
        var resolution = draw.Resolution;
        var actions = ToActionsDto(DrawActionEvaluator.EvaluateForRead(actionContext, draw, competitionStatus));
        var display = StageDrawDisplay.Project(
            draw,
            stage,
            actionContext.CompetitionStages,
            entries,
            slotDrawDisplayMetrics);
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
                IsApplied: false,
                IsObsoleteForConsumption: DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage),
                actions,
                draw.InputsIntent,
                display.Metrics,
                display.Cells);
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
                        side.PrimaryColor,
                        DrawPlacementOrigin.Draw);
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
            DrawAppliedState.IsApplied(draw, stage),
            DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage),
            actions,
            draw.InputsIntent,
            display.Metrics,
            display.Cells);
    }

    private static StageDrawActionsDto ToActionsDto(DrawActionsAssessment assessment) =>
        new(
            new StageDrawActionDto(assessment.Cancel.State, assessment.Cancel.BlockCode),
            new StageDrawReleaseActionDto(
                assessment.Release.State,
                assessment.Release.BlockCode,
                assessment.Release.ReleasableCount),
            new StageDrawActionDto(assessment.Apply.State, assessment.Apply.BlockCode),
            new StageDrawActionDto(assessment.PublishAndApply.State, assessment.PublishAndApply.BlockCode),
            new StageDrawActionDto(assessment.Redo.State, assessment.Redo.BlockCode));
}
