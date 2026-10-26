// -----------------------------------------------------------------------
// <copyright file="StructureEndpoints.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class StructureEndpoints
{
    public static IEndpointRouteBuilder MapStructureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/competitions/{competitionId:guid}/structure",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .GetStructureViewAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/structure",
            async (
                    Guid competitionId,
                    ConfigureStructureRequest request,
                    UseCaseExecutor executor,
                    CancellationToken cancellationToken) =>
                await EndpointHttpHelpers.ConfigureStructureHttpAsync(competitionId, request, executor, cancellationToken)
                    .ConfigureAwait(false));

        app.MapPost(
            "/competitions/{competitionId:guid}/stages",
            async (
                Guid competitionId,
                AddCompetitionStageRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var intent = StructureRequestMapper.ToStructureIntent(request);
                var (stage, view) = await executor
                    .AddCompetitionStageAsync(new CompetitionId(competitionId), intent, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stage.Id.Value}",
                    new AddCompetitionStageResponse(stage.Id.Value, stage.Name.Value, view));
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/stages/insert",
            async (
                Guid competitionId,
                InsertCompetitionStageRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var intent = StructureRequestMapper.ToStructureIntent(request);
                var position = StructureRequestMapper.ToStageInsertPosition(request.Position);
                var (stage, view) = await executor
                    .InsertCompetitionStageAsync(
                        new CompetitionId(competitionId),
                        intent,
                        new StageId(request.AnchorStageId),
                        position,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created(
                    $"/stages/{stage.Id.Value}",
                    new InsertCompetitionStageResponse(stage.Id.Value, stage.Name.Value, view));
            });

        app.MapPut(
            "/competitions/{competitionId:guid}/stages/order",
            async (
                Guid competitionId,
                SetCompetitionStageOrderRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var orderedStageIds = StructureRequestMapper.ToOrderedStageIds(request);
                var view = await executor
                    .SetCompetitionStageOrderAsync(
                        new CompetitionId(competitionId),
                        orderedStageIds,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPut(
            "/stages/{stageId:guid}/structure",
            async (
                Guid stageId,
                RebuildStageStructureRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var (impact, view) = await executor
                    .RebuildStageStructureAsync(
                        new StageId(stageId),
                        currentName => StructureRequestMapper.ToStructureIntent(request, currentName),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(
                    new RebuildStageStructureResponse(
                        new StructureRebuildImpactDto(
                            impact.ClearedMatchdays,
                            impact.ClearedGroups,
                            impact.ClearedRounds,
                            impact.ClearedSlots,
                            impact.ClearedDirectAssignments,
                            impact.ClearedCompositionEntries,
                            impact.ClearedDrawRules,
                            impact.ClearedSwissSettings),
                        view));
            });

        return app;
    }
}
