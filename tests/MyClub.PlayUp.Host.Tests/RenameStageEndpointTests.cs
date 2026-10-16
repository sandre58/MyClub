// -----------------------------------------------------------------------
// <copyright file="RenameStageEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class RenameStageEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Rename_when_Running_returns_204Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            RenameUri(stageId),
            new RenameStageRequest("Renamed"),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = await stages.GetByIdForUpdateAsync(stageId);
        loaded!.Name.Value.Should().Be("Renamed");
    }

    [IntegrationFact]
    public async Task Rename_when_Completed_returns_409_DisplayNameLockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedCompletedAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            RenameUri(stageId),
            new RenameStageRequest("Blocked"),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(StageErrorCodes.DisplayNameLocked);
    }

    private static Uri RenameUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/rename", UriKind.Relative);

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<StageId> SeedRunningAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Rename Running", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }

    private async Task<StageId> SeedCompletedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Rename Completed", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        situation.RequirePrimaryStage().Complete(_clock);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }
}
