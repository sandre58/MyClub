// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesStructureNotMutableEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceStageQualificationRulesStructureNotMutableEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Put_qualification_rules_on_running_stage_returns_StructureNotMutableAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client);
        using (var entry = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries",
            new AddEntryRequest("Alpha")))
        {
            entry.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var stageResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages",
            new AddCompetitionStageRequest("Championship", "Only"));
        stageResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var stage = (await stageResponse.Content.ReadFromJsonAsync<AddCompetitionStageResponse>(HostJson.Options))!;

        using (var prepare = await client.PostAsync($"/competitions/{competitionId}/prepare", content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var startCompetition = await client.PostAsync($"/competitions/{competitionId}/start", content: null))
        {
            startCompetition.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var startStage = await client.PostAsync($"/stages/{stage.StageId}/start", content: null))
        {
            startStage.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await client.PutAsJsonAsync(
            $"/stages/{stage.StageId}/qualification-rules",
            new ReplaceStageQualificationRulesRequest([]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    private static async Task<Guid> CreateCompetitionAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"Qual guard {Guid.CreateVersion7():N}"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        return created!.Id;
    }

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };
}
