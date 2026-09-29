// -----------------------------------------------------------------------
// <copyright file="MatchEndpoints.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/matches/{matchId:guid}",
            async (Guid matchId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var detail = await executor
                    .GetMatchDetailAsync(new MatchId(matchId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(detail);
            });

        app.MapPost(
            "/matches/{matchId:guid}/start",
            async (Guid matchId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor.StartMatchAsync(new MatchId(matchId), cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/matches/{matchId:guid}/finish",
            async (Guid matchId, FinishMatchRequest request, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var result = request.ToDomain();
                await executor.FinishMatchAsync(new MatchId(matchId), result, cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/matches/{matchId:guid}/declared-participations",
            async (
                Guid matchId,
                AddDeclaredParticipationRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .AddDeclaredParticipationAsync(
                        new MatchId(matchId),
                        new MemberId(request.MemberId),
                        request.Side,
                        request.CompositionStatus,
                        request.JerseyNumber,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapDelete(
            "/matches/{matchId:guid}/declared-participations/{memberId:guid}",
            async (Guid matchId, Guid memberId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .RemoveDeclaredParticipationAsync(new MatchId(matchId), new MemberId(memberId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/declared-participations/{memberId:guid}/composition-status",
            async (
                Guid matchId,
                Guid memberId,
                ChangeDeclaredParticipationCompositionStatusRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .ChangeDeclaredParticipationCompositionStatusAsync(
                        new MatchId(matchId),
                        new MemberId(memberId),
                        request.CompositionStatus,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/declared-participations/{memberId:guid}/jersey-number",
            async (
                Guid matchId,
                Guid memberId,
                SetDeclaredParticipationJerseyNumberRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .SetDeclaredParticipationJerseyNumberAsync(
                        new MatchId(matchId),
                        new MemberId(memberId),
                        request.JerseyNumber,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/running-score",
            async (
                Guid matchId,
                SetRunningScoreRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .SetRunningScoreAsync(
                        new MatchId(matchId),
                        request.HomeGoals,
                        request.AwayGoals,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/matches/{matchId:guid}/recorded-goals",
            async (
                Guid matchId,
                RecordGoalRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .RecordGoalAsync(
                        new MatchId(matchId),
                        new MemberId(request.ScorerMemberId),
                        request.CreditedSide,
                        request.AssisterMemberId is { } assister ? new MemberId(assister) : null,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/recorded-goals/{goalId:guid}",
            async (
                Guid matchId,
                Guid goalId,
                RecordGoalRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .CorrectRecordedGoalAsync(
                        new MatchId(matchId),
                        new GoalId(goalId),
                        new MemberId(request.ScorerMemberId),
                        request.CreditedSide,
                        request.AssisterMemberId is { } assister ? new MemberId(assister) : null,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapDelete(
            "/matches/{matchId:guid}/recorded-goals/{goalId:guid}",
            async (Guid matchId, Guid goalId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .RemoveRecordedGoalAsync(new MatchId(matchId), new GoalId(goalId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/matches/{matchId:guid}/recorded-substitutions",
            async (
                Guid matchId,
                RecordSubstitutionRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .RecordSubstitutionAsync(
                        new MatchId(matchId),
                        new MemberId(request.OutMemberId),
                        new MemberId(request.InMemberId),
                        request.Side,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/recorded-substitutions/{substitutionId:guid}",
            async (
                Guid matchId,
                Guid substitutionId,
                RecordSubstitutionRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .CorrectRecordedSubstitutionAsync(
                        new MatchId(matchId),
                        new SubstitutionId(substitutionId),
                        new MemberId(request.OutMemberId),
                        new MemberId(request.InMemberId),
                        request.Side,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapDelete(
            "/matches/{matchId:guid}/recorded-substitutions/{substitutionId:guid}",
            async (Guid matchId, Guid substitutionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .RemoveRecordedSubstitutionAsync(
                        new MatchId(matchId),
                        new SubstitutionId(substitutionId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/matches/{matchId:guid}/recorded-disciplinary-events",
            async (
                Guid matchId,
                RecordDisciplinaryEventRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .RecordDisciplinaryEventAsync(
                        new MatchId(matchId),
                        new MemberId(request.MemberId),
                        request.Type,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPut(
            "/matches/{matchId:guid}/recorded-disciplinary-events/{disciplinaryEventId:guid}",
            async (
                Guid matchId,
                Guid disciplinaryEventId,
                RecordDisciplinaryEventRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                await executor
                    .CorrectRecordedDisciplinaryEventAsync(
                        new MatchId(matchId),
                        new DisciplinaryEventId(disciplinaryEventId),
                        new MemberId(request.MemberId),
                        request.Type,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapDelete(
            "/matches/{matchId:guid}/recorded-disciplinary-events/{disciplinaryEventId:guid}",
            async (Guid matchId, Guid disciplinaryEventId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .RemoveRecordedDisciplinaryEventAsync(
                        new MatchId(matchId),
                        new DisciplinaryEventId(disciplinaryEventId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        return app;
    }
}
