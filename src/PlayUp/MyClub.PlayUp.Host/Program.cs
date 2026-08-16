// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlayUp")
    ?? throw new InvalidOperationException("Connection string 'PlayUp' is not configured.");

builder.Services.AddPlayUpInfrastructure(connectionString);
builder.Services.AddScoped<UseCaseExecutor>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PlayUpExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

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
    "/competitions/{competitionId:guid}/organisation",
    async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var view = await executor
            .GetOrganisationViewAsync(new CompetitionId(competitionId), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(view);
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
    "/competitions/{competitionId:guid}/entries/{entryId:guid}/exclude",
    async (
        Guid competitionId,
        Guid entryId,
        UseCaseExecutor executor,
        CancellationToken cancellationToken) =>
    {
        var view = await executor
            .ExcludeEntryAsync(
                new CompetitionId(competitionId),
                new EntryId(entryId),
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
                OrganisationRequestMapper.ToRegulation(request),
                cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(view);
    });

app.MapPost(
    "/competitions/{competitionId:guid}/organisation/structure",
    async (
        Guid competitionId,
        ConfigureStructureRequest request,
        UseCaseExecutor executor,
        CancellationToken cancellationToken) =>
    {
        var intent = OrganisationRequestMapper.ToStructureIntent(request);
        var view = await executor
            .ConfigureStructureAsync(new CompetitionId(competitionId), intent, cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(view);
    });

app.MapGet(
    "/competitions/{competitionId:guid}",
    async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var overview = await executor
            .GetCompetitionOverviewAsync(new CompetitionId(competitionId), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(overview);
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
    "/stages/{stageId:guid}/matches",
    async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var matches = await executor
            .ListMatchesByStageAsync(new StageId(stageId), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(matches);
    });

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
        return Results.Ok(new
        {
            appliedCount = applied.Count,
            assignments = applied.Select(instruction => new
            {
                stageId = instruction.StageId.Value,
                slotKey = instruction.SlotKey,
                entryId = instruction.EntryId.Value
            }).ToArray()
        });
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

// Match is an independent aggregate: routes are Match-centric (executor loads by MatchId only).
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
    "/stages/{stageId:guid}/draws/{drawId:guid}/publish",
    async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        await executor
            .PublishDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
            .ConfigureAwait(false);
        return Results.NoContent();
    });

app.MapPost(
    "/stages/{stageId:guid}/draws/{drawId:guid}/apply",
    async (
        Guid stageId,
        Guid drawId,
        ApplyDrawRequest? request,
        UseCaseExecutor executor,
        CancellationToken cancellationToken) =>
    {
        IReadOnlyList<FixtureId>? fixtureIds = request?.FixtureIds is { Count: > 0 } ids
            ? [.. ids.Select(id => new FixtureId(id))]
            : null;
        await executor
            .ApplyDrawAsync(new StageId(stageId), new DrawId(drawId), fixtureIds, cancellationToken)
            .ConfigureAwait(false);
        return Results.NoContent();
    });

app.MapPost(
    "/stages/{stageId:guid}/draws",
    async (Guid stageId, CreateDrawRequest request, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var kind = ParseDrawKind(request.Kind);
        var summary = await executor
            .CreateDrawAsync(new StageId(stageId), kind, cancellationToken)
            .ConfigureAwait(false);
        return Results.Created($"/stages/{stageId}/draws/{summary.DrawId}", summary);
    });

app.MapPost(
    "/stages/{stageId:guid}/draws/{drawId:guid}/inputs",
    async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var summary = await executor
            .ConfigureDrawInputsAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
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
        return Results.Ok(new
        {
            createdCount = result.CreatedMatches.Count,
            attachedMatchIds = result.AttachedMatchIds.Select(id => id.Value).ToArray(),
            alreadyComplete = result.AlreadyComplete
        });
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

app.Run();

static DrawResolutionKind ParseDrawKind(string kind)
{
    if (kind.Equals("Slot", StringComparison.OrdinalIgnoreCase))
    {
        return DrawResolutionKind.Slot;
    }

    if (kind.Equals("Group", StringComparison.OrdinalIgnoreCase)
        || kind.Equals("Groups", StringComparison.OrdinalIgnoreCase))
    {
        return DrawResolutionKind.Group;
    }

    if (kind.Equals("Pairing", StringComparison.OrdinalIgnoreCase)
        || kind.Equals("Cup", StringComparison.OrdinalIgnoreCase))
    {
        return DrawResolutionKind.Pairing;
    }

    throw new ApplicationFailureException(
        $"Unknown draw kind '{kind}'. Expected Slot, Group, or Pairing.",
        ApplicationErrorCodes.DrawKindNotSupported);
}
