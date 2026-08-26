// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
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
builder.Services.ConfigureHttpJsonOptions(static options =>

    // Phase 12.8: HTTP enums as JSON strings (camelCase property names unchanged).
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
    "/competitions/{competitionId:guid}/cockpit",
    async (Guid competitionId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        var cockpit = await executor
            .GetCockpitViewAsync(new CompetitionId(competitionId), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(cockpit);
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
                request.ShortName,
                request.LogoPath,
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
                request.LogoPath,
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
                request.LogoPath,
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

app.MapPost(
    "/competitions/{competitionId:guid}/stages",
    async (
        Guid competitionId,
        AddCompetitionStageRequest request,
        UseCaseExecutor executor,
        CancellationToken cancellationToken) =>
    {
        ArgumentNullException.ThrowIfNull(request);
        var stage = await executor
            .AddCompetitionStageAsync(new CompetitionId(competitionId), request.Name, cancellationToken)
            .ConfigureAwait(false);
        return Results.Created(
            $"/stages/{stage.Id.Value}",
            new AddCompetitionStageResponse(stage.Id.Value, stage.Name.Value));
    });

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
    "/stages/{stageId:guid}/progression-rules",
    async (
        Guid stageId,
        ReplaceStageProgressionRulesRequest request,
        UseCaseExecutor executor,
        CancellationToken cancellationToken) =>
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<ProgressionPathSpec>? paths = null;
        if (request.Paths is { Count: > 0 })
        {
            paths = request.Paths
                .Select(path => new ProgressionPathSpec(
                    new FixtureId(path.SourceFixtureId),
                    path.Outcome,
                    new StageId(path.DestinationStageId),
                    path.DestinationSlotKey))
                .ToArray();
        }

        await executor
            .ReplaceStageProgressionRulesAsync(new StageId(stageId), paths, cancellationToken)
            .ConfigureAwait(false);
        return Results.NoContent();
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
        return Results.Ok(new QualificationApplyResponse(
            applied.Count,
            [
                .. applied.Select(instruction => new QualificationAssignmentDto(
                    instruction.StageId.Value,
                    instruction.SlotKey,
                    instruction.EntryId.Value))
            ]));
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
        var mode = parseCompletionMode(request.Mode);
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
        var kind = parseDrawKind(request.Kind);
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
        return Results.Ok(new MaterializeMatchesResponse(
            result.CreatedMatches.Count,
            [.. result.AttachedMatchIds.Select(id => id.Value)],
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
        var pairs = request.Pairs
            .Select(pair => new CupSlotPair(pair.SlotAKey, pair.SlotBKey))
            .ToArray();
        var result = await executor
            .MaterializeCupFromOccupiedSlotsAsync(new StageId(stageId), pairs, cancellationToken)
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

await app.RunAsync().ConfigureAwait(false);
return;

static CompletionMode parseCompletionMode(string mode) => mode.Equals("Normal", StringComparison.OrdinalIgnoreCase)
    ? CompletionMode.Normal
    : mode.Equals("Administrative", StringComparison.OrdinalIgnoreCase)
        ? CompletionMode.Administrative
        : mode.Equals("Abandoned", StringComparison.OrdinalIgnoreCase)
            ? CompletionMode.Abandoned
            : throw new ApplicationFailureException(
                $"Unknown completion mode '{mode}'. Expected Normal, Administrative, or Abandoned.",
                ApplicationErrorCodes.InvalidCompletionMode);

static DrawResolutionKind parseDrawKind(string kind) => kind.Equals("Slot", StringComparison.OrdinalIgnoreCase)
    ? DrawResolutionKind.Slot
    : kind.Equals("Group", StringComparison.OrdinalIgnoreCase)
      || kind.Equals("Groups", StringComparison.OrdinalIgnoreCase)
        ? DrawResolutionKind.Group
        : kind.Equals("Pairing", StringComparison.OrdinalIgnoreCase)
          || kind.Equals("Cup", StringComparison.OrdinalIgnoreCase)
            ? DrawResolutionKind.Pairing
            : throw new ApplicationFailureException(
                $"Unknown draw kind '{kind}'. Expected Slot, Group, or Pairing.",
                ApplicationErrorCodes.DrawKindNotSupported);
