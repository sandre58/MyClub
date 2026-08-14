// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Host;
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
    "/stages/{stageId:guid}/prepare",
    async (Guid stageId, UseCaseExecutor executor, CancellationToken cancellationToken) =>
    {
        await executor.PrepareStageAsync(new StageId(stageId), cancellationToken).ConfigureAwait(false);
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

app.Run();
