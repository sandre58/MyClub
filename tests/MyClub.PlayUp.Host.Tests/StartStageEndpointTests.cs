// -----------------------------------------------------------------------
// <copyright file="StartStageEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class StartStageEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Start_returns_204_and_persists_RunningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedReadyChampionshipAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = await stages.GetByIdAsync(stageId);
        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(StageStatus.Running);
    }

    [IntegrationFact]
    public async Task Start_when_missing_returns_404_ProblemDetailsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var missingId = StageId.New();

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(missingId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Start_when_Draft_returns_409_ProblemDetailsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedDraftChampionshipAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(StageErrorCodes.InvalidTransition);
    }

    private static Uri StartUri(StageId stageId) => new($"/stages/{stageId.Value}/start", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<StageId> SeedDraftChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Championnat Start Draft"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }

    private async Task<StageId> SeedReadyChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Championnat Start Ready"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }
}
