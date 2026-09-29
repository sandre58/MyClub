// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Serialization;
using MyClub.Media.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Host;
using MyClub.PlayUp.Host.Endpoints;
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
        options.CustomizeProblemDetails = static context =>
            context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.GetCorrelationId());
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

    app.MapMediaEndpoints();
    app.MapCompetitionEndpoints();
    app.MapStructureEndpoints();
    app.MapStageEndpoints();
    app.MapMatchEndpoints();

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
