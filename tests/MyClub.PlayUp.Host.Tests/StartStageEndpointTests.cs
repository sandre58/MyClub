// -----------------------------------------------------------------------
// <copyright file="StartStageEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
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
        var loaded = await stages.GetByIdForUpdateAsync(stageId);
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
        var situation = TestCompetition.Create("Championnat Start Draft", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }

    private async Task<StageId> SeedReadyChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Championnat Start Ready", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }
}
