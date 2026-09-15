// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Serialization;
using MyClub.Media.Application.Media;
using MyClub.Media.Domain;
using MyClub.Media.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(static (context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();

        if (context.HostingEnvironment.IsDevelopment())
        {
            configuration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            configuration.WriteTo.Console(new CompactJsonFormatter());
        }
    });

    var connectionString = builder.Configuration.GetConnectionString("PlayUp")
                           ?? throw new InvalidOperationException("Connection string 'PlayUp' is not configured.");

    var mediaConnectionString = builder.Configuration.GetConnectionString("Media") ?? connectionString;
    var mediaStorageRoot = builder.Configuration["Media:StorageRoot"]
                           ?? Path.Combine(builder.Environment.ContentRootPath, ".local", "media");
    if (!Path.IsPathRooted(mediaStorageRoot))
    {
        mediaStorageRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, mediaStorageRoot));
    }

    builder.Services.AddPlayUpInfrastructure(connectionString);
    builder.Services.AddMediaInfrastructure(mediaConnectionString, mediaStorageRoot);
    builder.Services.AddScoped<IMediaReferenceChecker, MediaReferenceChecker>();
    builder.Services.AddScoped<UseCaseExecutor>();
    builder.Services.ConfigureHttpJsonOptions(static options =>

        // Phase 12.8: HTTP enums as JSON strings (camelCase property names unchanged).
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails(static options =>
    {
        // Framework-produced ProblemDetails only — manual handlers add correlationId themselves.
        options.CustomizeProblemDetails = static context =>
        {
            context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.GetCorrelationId();
        };
    });
    builder.Services.AddExceptionHandler<MediaExceptionHandler>();
    builder.Services.AddExceptionHandler<PlayUpExceptionHandler>();

    var app = builder.Build();

    Log.Information("Play'Up Host starting");

// Request logging wraps ExceptionHandler so completed status (incl. mapped 4xx/5xx) is logged once
// at Information — handlers own Error for server failures; avoids a second Error from Serilog.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(static options =>
    {
        options.GetLevel = static (_, _, exception) =>
            exception is null ? LogEventLevel.Information : LogEventLevel.Error;
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });
    app.UseExceptionHandler();

    app.MapPost(
        "/media",
        async (HttpRequest request, MediaService mediaService, CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { title = "Expected multipart/form-data with a 'file' field." });
            }

            var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { title = "A non-empty 'file' form field is required." });
            }

            await using var stream = file.OpenReadStream();
            var metadata = await mediaService
                .CreateAsync(stream, file.ContentType, file.Length, file.FileName, cancellationToken)
                .ConfigureAwait(false);

            return Results.Created(
                $"/media/{metadata.Id}",
                new MediaMetadataResponse(
                    metadata.Id,
                    metadata.ContentType,
                    metadata.ByteSize,
                    metadata.OriginalName,
                    metadata.CreatedAt));
        });

    app.MapGet(
        "/media/{mediaId:guid}",
        async (Guid mediaId, MediaService mediaService, CancellationToken cancellationToken) =>
        {
            var metadata = await mediaService
                .GetMetadataAsync(new MediaId(mediaId), cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(
                new MediaMetadataResponse(
                    metadata.Id,
                    metadata.ContentType,
                    metadata.ByteSize,
                    metadata.OriginalName,
                    metadata.CreatedAt));
        });

    app.MapGet(
        "/media/{mediaId:guid}/content",
        async (Guid mediaId, MediaService mediaService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var content = await mediaService
                .OpenContentAsync(new MediaId(mediaId), cancellationToken)
                .ConfigureAwait(false);

            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return Results.File(
                content.Content,
                content.ContentType,
                enableRangeProcessing: false);
        });

    app.MapDelete(
        "/media/{mediaId:guid}",
        async (Guid mediaId, MediaService mediaService, CancellationToken cancellationToken) =>
        {
            await mediaService.DeleteAsync(new MediaId(mediaId), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

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

    app.MapPost(
        "/competitions/{competitionId:guid}/structure",
        async (
            Guid competitionId,
            ConfigureStructureRequest request,
            UseCaseExecutor executor,
            CancellationToken cancellationToken) =>
        {
            return await ConfigureStructureHttpAsync(competitionId, request, executor, cancellationToken)
                .ConfigureAwait(false);
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
            var intent = StructureRequestMapper.ToStructureIntent(request);
            var (stage, view) = await executor
                .AddCompetitionStageAsync(new CompetitionId(competitionId), intent, cancellationToken)
                .ConfigureAwait(false);
            return Results.Created(
                $"/stages/{stage.Id.Value}",
                new AddCompetitionStageResponse(stage.Id.Value, stage.Name.Value, view));
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
            IReadOnlyList<ProgressionPathSpec>? paths = null;
            if (request.Paths is { Count: > 0 })
            {
                paths =
                [
                    .. request.Paths
                        .Select(path => new ProgressionPathSpec(
                            new FixtureId(path.SourceFixtureId),
                            path.Outcome,
                            new StageId(path.DestinationStageId),
                            path.DestinationSlotKey))
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
                        path.DestinationSlotKey,
                        path.RankingScope,
                        path.GroupId,
                        path.AcrossGroupsPosition,
                        path.SelectionEndValue,
                        path.MinimumPoints))
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
                        .Select(path => new PlacementAwardPathSpec(
                            new FixtureId(path.SourceFixtureId),
                            path.Outcome,
                            path.Rank))
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

                SeedingRules? seeding = request.NumberOfSeeds is null
                    ? null
                    : new SeedingRules(request.NumberOfSeeds.Value);
                PotRules? pots = request.NumberOfPots is null
                    ? null
                    : new PotRules(request.NumberOfPots.Value);
                drawRules = new DrawRules(request.Mode.Value, seeding, pots);
            }

            await executor
                .ReplaceStageDrawRulesAsync(new StageId(stageId), drawRules, cancellationToken)
                .ConfigureAwait(false);
            return Results.NoContent();
        });

    app.MapPut(
        "/stages/{stageId:guid}/composition",
        async (
            Guid stageId,
            EntryIdsRequest request,
            UseCaseExecutor executor,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);
            var entryIds = (request.EntryIds ?? [])
                .Select(id => new EntryId(id))
                .ToArray();
            await executor
                .ReplaceStageCompositionEntriesAsync(new StageId(stageId), entryIds, cancellationToken)
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
        "/stages/{stageId:guid}/draws/{drawId:guid}/cancel",
        async (Guid stageId, Guid drawId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
        {
            await executor
                .CancelDrawAsync(new StageId(stageId), new DrawId(drawId), cancellationToken)
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
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is thrown by EF Core design-time tools after resolving the host —
    // do not treat it as a fatal startup failure.
    Log.Fatal(exception, "Play'Up Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

return;

static async Task<IResult> ConfigureStructureHttpAsync(
    Guid competitionId,
    ConfigureStructureRequest request,
    UseCaseExecutor executor,
    CancellationToken cancellationToken)
{
    var intent = StructureRequestMapper.ToStructureIntent(request);
    var (result, view) = await executor
        .ConfigureStructureAsync(new CompetitionId(competitionId), intent, cancellationToken)
        .ConfigureAwait(false);
    StructureRebuildImpactDto? impact = result.RebuildImpact is null
        ? null
        : new StructureRebuildImpactDto(
            result.RebuildImpact.ClearedMatchdays,
            result.RebuildImpact.ClearedGroups,
            result.RebuildImpact.ClearedRounds,
            result.RebuildImpact.ClearedSlots,
            result.RebuildImpact.ClearedDirectAssignments,
            result.RebuildImpact.ClearedCompositionEntries,
            result.RebuildImpact.ClearedDrawRules,
            result.RebuildImpact.ClearedSwissSettings);
    return Results.Ok(new ConfigureStructureResponse(result.StageCreated, impact, view));
}

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
