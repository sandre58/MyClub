// -----------------------------------------------------------------------
// <copyright file="CompetitionEndpoints.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class CompetitionEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/competitions",
            async (CreateCompetitionRequest request, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var summary = await executor
                    .CreateCompetitionAsync(request.Name, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Created($"/competitions/{summary.Id}/workspace", summary);
            });

        app.MapGet(
            "/competitions",
            async (UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var list = await executor.ListCompetitionsAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(list);
            });

        app.MapGet(
            "/competitions/{competitionId:guid}/workspace",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var summary = await executor
                    .GetWorkspaceSummaryAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(summary);
            });

        app.MapGet(
            "/competitions/{competitionId:guid}/overview",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var overview = await executor
                    .GetOverviewViewAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(overview);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries",
            async (
                Guid competitionId,
                AddEntryRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .AddEntryAsync(
                        new CompetitionId(competitionId),
                        request.DisplayName,
                        request.TeamId,
                        request.ShortName,
                        request.LogoMediaId,
                        request.PrimaryColor,
                        request.SecondaryColor,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/presentation",
            async (
                Guid competitionId,
                UpdateCompetitionPresentationRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .UpdateCompetitionPresentationAsync(
                        new CompetitionId(competitionId),
                        request.ShortName,
                        request.LogoMediaId,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/schedule",
            async (
                Guid competitionId,
                SetCompetitionScheduleRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .SetCompetitionScheduleAsync(
                        new CompetitionId(competitionId),
                        request.ScheduledStart,
                        request.ScheduledEnd,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/presentation",
            async (
                Guid competitionId,
                Guid entryId,
                UpdateEntryPresentationRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .UpdateEntryPresentationAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        request.ShortName,
                        request.LogoMediaId,
                        request.PrimaryColor,
                        request.SecondaryColor,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/rename",
            async (
                Guid competitionId,
                Guid entryId,
                RenameEntryRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .RenameEntryAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        request.DisplayName,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/withdraw",
            async (
                Guid competitionId,
                Guid entryId,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .WithdrawEntryAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/delete",
            async (
                Guid competitionId,
                Guid entryId,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .DeleteEntryAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entry-lots/delete",
            async (
                Guid competitionId,
                EntryIdsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .DeleteEntriesAsync(
                        new CompetitionId(competitionId),
                        [.. request.EntryIds.Select(id => new EntryId(id))],
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entry-lots/withdraw",
            async (
                Guid competitionId,
                EntryIdsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .WithdrawEntriesAsync(
                        new CompetitionId(competitionId),
                        [.. request.EntryIds.Select(id => new EntryId(id))],
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/declared-members",
            async (
                Guid competitionId,
                Guid entryId,
                AddDeclaredMemberRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .AddDeclaredMemberAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        request.DisplayName,
                        request.Role,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapDelete(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/declared-members/{memberId:guid}",
            async (
                Guid competitionId,
                Guid entryId,
                Guid memberId,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .RemoveDeclaredMemberAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        new MemberId(memberId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/declared-member-lots/remove",
            async (
                Guid competitionId,
                Guid entryId,
                MemberIdsRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .RemoveDeclaredMembersAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        [.. request.MemberIds.Select(id => new MemberId(id))],
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/entries/{entryId:guid}/declared-members/{memberId:guid}/rename",
            async (
                Guid competitionId,
                Guid entryId,
                Guid memberId,
                RenameDeclaredMemberRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .RenameDeclaredMemberAsync(
                        new CompetitionId(competitionId),
                        new EntryId(entryId),
                        new MemberId(memberId),
                        request.DisplayName,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapPut(
            "/competitions/{competitionId:guid}/regulation",
            async (
                Guid competitionId,
                ReplaceRegulationRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var view = await executor
                    .ReplaceRegulationAsync(
                        new CompetitionId(competitionId),
                        existing => StructureRequestMapper.ToRegulation(
                            request,
                            existing.DisciplinaryRules),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(view);
            });

        app.MapDelete(
            "/competitions/{competitionId:guid}/stages/{stageId:guid}",
            async (
                Guid competitionId,
                Guid stageId,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var (impact, view) = await executor
                    .RemoveCompetitionStageAsync(
                        new CompetitionId(competitionId),
                        new StageId(stageId),
                        cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(
                    new RemoveCompetitionStageResponse(
                        impact.RemovedStageId.Value,
                        impact.ScrubbedQualificationPaths,
                        impact.ScrubbedProgressionPaths,
                        view));
            });

        app.MapGet(
            "/competitions/{competitionId:guid}",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var overview = await executor
                    .GetCompetitionDetailAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(overview);
            });

        app.MapGet(
            "/competitions/{competitionId:guid}/matches-hub",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var hub = await executor
                    .GetMatchHubViewAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(hub);
            });

        app.MapGet(
            "/competitions/{competitionId:guid}/attention",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var attention = await executor
                    .GetNeedsAttentionAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(attention);
            });

        app.MapGet(
            "/competitions/{competitionId:guid}/consultation",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                var consultation = await executor
                    .GetConsultationAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(consultation);
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/prepare",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .PrepareCompetitionAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/start",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .StartCompetitionAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/complete",
            async (
                Guid competitionId,
                CompleteCompetitionRequest request,
                UseCaseExecutor executor,
                CancellationToken cancellationToken) =>
            {
                var mode = EndpointHttpHelpers.ParseCompletionMode(request.Mode);
                await executor
                    .CompleteCompetitionAsync(new CompetitionId(competitionId), mode, cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        app.MapPost(
            "/competitions/{competitionId:guid}/archive",
            async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
            {
                await executor
                    .ArchiveCompetitionAsync(new CompetitionId(competitionId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            });

        return app;
    }
}
