// -----------------------------------------------------------------------
// <copyright file="StageEndpoints.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class StageEndpoints
{
    public static IEndpointRouteBuilder MapStageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/stages/{stageId:guid}/rounds",
            async (
                Guid stageId,
                AddStageRoundRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var round = await executor
                    .AddStageRoundAsync(
                        new StageId(stageId),
                        request.Name,
                        request.NumberOfLegs,
                        request.AggregateScoring,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stageId}/rounds/{round.Id.Value}",
                    new AddStageRoundResponse(round.Id.Value, round.Name));
            });

        app.MapPost(
            "/stages/{stageId:guid}/slots",
            async (
                Guid stageId,
                AddStageSlotRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var slot = await executor
                    .AddStageSlotAsync(new StageId(stageId), request.SlotKey, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stageId}/slots/{Uri.EscapeDataString(slot.SlotKey)}",
                    new AddStageSlotResponse(slot.SlotKey));
            });

        app.MapPut(
            "/stages/{stageId:guid}/slots/{slotKey}/assignment",
            async (
                Guid stageId,
                string slotKey,
                AssignEntryToSlotRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                await executor
                    .AssignEntryToSlotAsync(
                        new StageId(stageId),
                        slotKey,
                        new EntryId(request.EntryId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapDelete(
            "/stages/{stageId:guid}/slots/{slotKey}/assignment",
            async (
                Guid stageId,
                string slotKey,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .ClearSlotAssignmentAsync(new StageId(stageId), slotKey, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/rename",
            async (
                Guid stageId,
                RenameStageRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                await executor
                    .RenameStageAsync(new StageId(stageId), request.Name, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/matchdays",
            async (
                Guid stageId,
                AddStageMatchdayRequest? request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var matchday = await executor
                    .AddStageMatchdayAsync(new StageId(stageId), request?.Number, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stageId}/matchdays/{matchday.Id.Value}",
                    new AddStageMatchdayResponse(matchday.Id.Value, matchday.Number));
            });

        app.MapPost(
            "/stages/{stageId:guid}/groups",
            async (
                Guid stageId,
                AddStageGroupRequest? request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var group = await executor
                    .AddStageGroupAsync(new StageId(stageId), request?.Name, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stageId}/groups/{group.Id.Value}",
                    new AddStageGroupResponse(group.Id.Value, group.Name));
            });

        app.MapPut(
            "/stages/{stageId:guid}/match-generation-format",
            async (
                Guid stageId,
                ReplaceStageMatchGenerationFormatRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                if (!Enum.TryParse<MatchGenerationFormat>(request.Format, ignoreCase: true, out var format)
                    || !Enum.IsDefined(format))
                {
                    return Results.BadRequest(new { code = ApplicationErrorCodes.InvalidStructureIntent });
                }

                await executor
                    .ReplaceStageMatchGenerationFormatAsync(new StageId(stageId), format, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/swiss-settings",
            async (
                Guid stageId,
                ReplaceStageSwissSettingsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                await executor
                    .ReplaceStageSwissSettingsAsync(new StageId(stageId), request.RoundCount, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/progression-rules",
            async (
                Guid stageId,
                ReplaceStageProgressionRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                if (request.Intents is { Count: > 0 })
                {
                    IReadOnlyList<ProgressionIntentSpec> intents =
                    [
                        .. request.Intents.Select(intent => new ProgressionIntentSpec(
                            intent.IntentId,
                            intent.Order,
                            new RoundId(intent.RoundId),
                            intent.Outcome,
                            new StageId(intent.DestinationStageId),
                            DestinationSlotKeys: EndpointHttpHelpers.CoerceDestinationSlotKeys(
                                intent.DestinationSlotKeys,
                                intent.DestinationSlotKey),
                            DestinationGroupIds: intent.DestinationGroupIds is { Count: > 0 } ? intent.DestinationGroupIds.Select(id => new GroupId(id)).ToArray() : null,
                            DestinationForm: intent.DestinationForm))
                    ];
                    await executor
                        .ReplaceStageProgressionIntentsAsync(new StageId(stageId), intents, cancellationToken)
                        .ConfigureAwait(false);
                    return Results.NoContent();
                }

                IReadOnlyList<ProgressionPathSpec>? paths = null;
                if (request.Paths is { Count: > 0 })
                {
                    paths =
                    [
                        .. request.Paths
                            .Select(path =>
                            {
                                var pairKey = path.SourcePairKey?.Trim();
                                if (string.IsNullOrEmpty(pairKey))
                                {
                                    throw new ArgumentException(
                                        "Progression path requires SourcePairKey.",
                                        nameof(request));
                                }

                                return new ProgressionPathSpec(
                                    pairKey,
                                    path.Outcome,
                                    new StageId(path.DestinationStageId),
                                    path.DestinationSlotKey,
                                    path.DestinationGroupId is { } gid ? new GroupId(gid) : null,
                                    path.DestinationForm);
                            })
                    ];
                }

                await executor
                    .ReplaceStageProgressionRulesAsync(new StageId(stageId), paths, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/qualification-rules",
            async (
                Guid stageId,
                ReplaceStageQualificationRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                if (request.Intents is { Count: > 0 })
                {
                    IReadOnlyList<QualificationIntentSpec> intents =
                    [
                        .. request.Intents.Select(intent => new QualificationIntentSpec(
                            intent.IntentId,
                            intent.Order,
                            intent.SourceKind,
                            intent.PositionFrom,
                            intent.PositionTo,
                            intent.DestinationStageId,
                            intent.GroupId,
                            intent.AcrossGroupsPosition,
                            intent.MinimumPoints,
                            EndpointHttpHelpers.CoerceDestinationSlotKeys(intent.DestinationSlotKeys, intent.DestinationSlotKey),
                            intent.DestinationGroupIds,
                            intent.DestinationForm))
                    ];
                    await executor
                        .ReplaceStageQualificationIntentsAsync(new StageId(stageId), intents, cancellationToken)
                        .ConfigureAwait(false);
                    return Results.NoContent();
                }

                IReadOnlyList<QualificationPathSpec>? paths = null;
                if (request.Paths is { Count: > 0 })
                {
                    paths =
                    [
                        .. request.Paths.Select(path => new QualificationPathSpec(
                            path.Order,
                            path.SelectionMode,
                            path.SelectionValue,
                            path.DestinationStageId,
                            path.RankingScope,
                            path.GroupId,
                            path.AcrossGroupsPosition,
                            path.SelectionEndValue,
                            path.MinimumPoints,
                            path.DestinationSlotKey,
                            path.DestinationGroupId,
                            path.DestinationForm))
                    ];
                }

                await executor
                    .ReplaceStageQualificationRulesAsync(new StageId(stageId), paths, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/placement-award-rules",
            async (
                Guid stageId,
                ReplaceStagePlacementAwardRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                IReadOnlyList<PlacementAwardPathSpec>? paths = null;
                if (request.Paths is { Count: > 0 })
                {
                    paths =
                    [
                        .. request.Paths
                            .Select(path =>
                            {
                                var pairKey = path.SourcePairKey?.Trim();
                                if (string.IsNullOrEmpty(pairKey))
                                {
                                    throw new ApplicationFailureException(
                                        "Placement award path requires SourcePairKey.",
                                        ApplicationErrorCodes.InvalidStructureIntent);
                                }

                                return new PlacementAwardPathSpec(pairKey, path.Outcome, path.Rank);
                            })
                    ];
                }

                await executor
                    .ReplaceStagePlacementAwardRulesAsync(new StageId(stageId), paths, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/standing-rules",
            async (
                Guid stageId,
                ReplaceStageStandingRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                ArgumentNullException.ThrowIfNull(request.RankingCriteria);

                var standingRules = new StandingRules(
                    new PointsPolicy(request.WinPoints, request.DrawPoints, request.LossPoints),
                    request.RankingCriteria);

                await executor
                    .ReplaceStageStandingRulesAsync(new StageId(stageId), standingRules, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/match-rules",
            async (
                Guid stageId,
                ReplaceStageMatchRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var matchRules = StructureRequestMapper.ToMatchRules(request);
                await executor
                    .ReplaceStageMatchRulesAsync(new StageId(stageId), matchRules, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/bind-to-competition",
            async (
                Guid stageId,
                BindStageRegulationRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                await executor
                    .BindStageRegulationAsync(new StageId(stageId), request.Scope, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/draw-rules",
            async (
                Guid stageId,
                ReplaceStageDrawRulesRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                DrawRules? drawRules = null;
                if (!request.Clear)
                {
                    if (request.Mode is null)
                    {
                        throw new ApplicationFailureException(
                            "DrawRules require Mode when Clear is false.",
                            ApplicationErrorCodes.InvalidStructureIntent);
                    }

                    var seeding = request.NumberOfSeeds is null
                        ? null
                        : new SeedingRules(request.NumberOfSeeds.Value);
                    var pots = request.NumberOfPots is null
                        ? null
                        : new PotRules(request.NumberOfPots.Value);

                    // Constraints omitted from the write contract — Application preserves
                    // any existing constraints on replace (see ReplaceStageDrawRules).
                    drawRules = new DrawRules(request.Mode.Value, seeding, pots);
                }

                await executor
                    .ReplaceStageDrawRulesAsync(new StageId(stageId), drawRules, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/affectation",
            async (
                Guid stageId,
                EntryIdsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var entryIds = request.EntryIds
                    .Select(id => new EntryId(id))
                    .ToArray();
                await executor
                    .ReplaceStageAffectationAuthoringAsync(new StageId(stageId), entryIds, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/tie-format",
            async (
                Guid stageId,
                ReplaceStageDefaultTieFormatRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                TieFormat? tieFormat = null;
                if (!request.Clear)
                {
                    var aggregate = request.NumberOfLegs == TieFormat.TwoLegs;
                    tieFormat = new TieFormat(
                        request.NumberOfLegs,
                        aggregate,
                        request.HasAwayGoalsRule ? new AwayGoalsRule() : null,
                        request.HasExtraTimeRule ? new ExtraTimeRule() : null,
                        request.HasPenaltyShootoutRule ? new PenaltyShootoutRule() : null);
                }

                await executor
                    .ReplaceStageDefaultTieFormatAsync(new StageId(stageId), tieFormat, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/stages/{stageId:guid}/rounds/{roundId:guid}/tie-format",
            async (
                Guid stageId,
                Guid roundId,
                ReplaceRoundTieFormatRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                TieFormat? tieFormat = null;
                if (!request.Clear)
                {
                    var aggregate = request.NumberOfLegs == TieFormat.TwoLegs;
                    tieFormat = new TieFormat(
                        request.NumberOfLegs,
                        aggregate,
                        request.HasAwayGoalsRule ? new AwayGoalsRule() : null,
                        request.HasExtraTimeRule ? new ExtraTimeRule() : null,
                        request.HasPenaltyShootoutRule ? new PenaltyShootoutRule() : null);
                }

                await executor
                    .ReplaceRoundTieFormatAsync(
                        new StageId(stageId),
                        new RoundId(roundId),
                        tieFormat,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapGet(
            "/stages/{stageId:guid}",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var overview = await executor
                    .GetStageOverviewAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(overview);
            });

        app.MapGet(
            "/stages/{stageId:guid}/schematic",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var schematic = await executor
                    .GetStageSchematicAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(schematic);
            });

        app.MapGet(
            "/stages/{stageId:guid}/matches",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var matches = await executor
                    .ListMatchesByStageAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(matches);
            });

        app.MapPost(
            "/stages/{stageId:guid}/prepare",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor.PrepareStageAsync(new StageId(stageId), cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/start",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor.StartStageAsync(new StageId(stageId), cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/fixtures/{fixtureId:guid}/apply-progression",
            async (Guid stageId, Guid fixtureId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .ApplyProgressionOutcomeAsync(new StageId(stageId), new FixtureId(fixtureId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/qualification/apply",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var applied = await executor
                    .ApplyQualificationAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new QualificationApplyResponse(
                    applied.Count,
                    [
                        .. applied.Select(instruction => new QualificationAssignmentDto(
                            instruction.StageId.Value,
                            instruction.EntryId.Value))
                    ]));
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/publish",
            async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .PublishDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/publish-and-apply",
            async (
                Guid stageId,
                Guid drawId,
                ApplyDrawRequest? _,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .PublishAndApplyDrawAsync(
                        new StageId(stageId),
                        new DrawId(drawId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/cancel",
            async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .CancelDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/release-aligned-placements",
            async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var result = await executor
                    .ReleaseDrawAlignedPlacementsAsync(
                        new StageId(stageId),
                        new DrawId(drawId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(result);
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/apply",
            async (
                Guid stageId,
                Guid drawId,
                ApplyDrawRequest? _,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .ApplyDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws",
            async (Guid stageId, CreateDrawRequest request, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var kind = EndpointHttpHelpers.ParseDrawKind(request.Kind);
                _ = EndpointHttpHelpers.ParseDrawInputsIntent(request.Intent); // validate early; inputs step applies intent
                var summary = await executor
                    .CreateDrawAsync(new StageId(stageId), kind, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created($"/stages/{stageId}/draws/{summary.DrawId}", summary);
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/inputs",
            async (
                Guid stageId,
                Guid drawId,
                ConfigureDrawInputsRequest? request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var intent = EndpointHttpHelpers.ParseDrawInputsIntent(request?.Intent);
                var summary = await executor
                    .ConfigureDrawInputsAsync(
                        new StageId(stageId),
                        new DrawId(drawId),
                        intent,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(summary);
            });

        app.MapPost(
            "/stages/{stageId:guid}/draws/{drawId:guid}/generate",
            async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var result = await executor
                    .GenerateDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(result);
            });

        app.MapPost(
            "/stages/{stageId:guid}/matches/materialize",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var result = await executor
                    .MaterializeMatchesAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new MaterializeMatchesResponse(
                    result.CreatedMatches.Count,
                    [.. result.AttachedMatchIds.Select(id => id.Value)],
                    result.AlreadyComplete));
            });

        app.MapPost(
            "/stages/{stageId:guid}/swiss/generate-next-round",
            async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var result = await executor
                    .GenerateNextRoundAsync(new StageId(stageId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new GenerateNextRoundResponse(
                    result.RoundIndex,
                    result.CreatedMatches.Count,
                    [.. result.CreatedMatches.Select(match => match.Id.Value)],
                    result.ByeEntryId?.Value,
                    result.AlreadyComplete));
            });

        app.MapPost(
            "/stages/{stageId:guid}/matches/materialize-from-slots",
            async (
                Guid stageId,
                MaterializeCupFromOccupiedSlotsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var result = await executor
                    .MaterializeCupFromOccupiedSlotsAsync(new StageId(stageId), request.PairKeys, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new MaterializeMatchesResponse(
                    result.CreatedMatches.Count,
                    [.. result.AttachedMatchIds.Select(id => id.Value)],
                    result.AlreadyComplete));
            });

        app.MapPost(
            "/stages/{stageId:guid}/schedule/generate",
            async (
                Guid stageId,
                GenerateScheduleRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var proposal = await executor
                    .GenerateScheduleAsync(
                        new StageId(stageId),
                        request.HorizonStart,
                        request.HorizonEnd,
                        request.GranularityMinutes,
                        request.TimeZoneId,
                        request.TargetMatchIds,
                        request.ResourceIds,
                        request.MatchDurationMinutes,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(proposal);
            });

        app.MapPost(
            "/stages/{stageId:guid}/schedule/apply",
            async (
                Guid stageId,
                ApplyScheduleRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .ApplyScheduleAsync(
                        new StageId(stageId),
                        request.Assignments,
                        request.TargetMatchIds,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        return app;
    }
}
