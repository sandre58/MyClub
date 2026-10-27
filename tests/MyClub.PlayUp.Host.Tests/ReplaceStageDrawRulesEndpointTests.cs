// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDrawRulesEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// <c>ReplaceStageDrawRules</c> is Draft-only; Ready refuses via <see cref="StageErrorCodes.StructureLocked"/>.
/// Application proof: <c>ReplaceStageDrawRulesTests.Replace_rejected_when_stage_is_Ready</c>.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceStageDrawRulesEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Put_draw_rules_when_ready_returns_409_StructureLockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "B-Q1b-draw-rules");
        var entryIds = await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Cup", BracketSize: 4));
        structureResponse.EnsureSuccessStatusCode();
        var stageId = (await structureResponse.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
            .Structure.Format.PrimaryStageId!.Value;

        await SeedStageAffectationAsync(client, stageId, entryIds);

        using (var drawRulesDraft = await client.PutAsJsonAsync(
                   DrawRulesUri(stageId),
                   new ReplaceStageDrawRulesRequest(Clear: false, Mode: DrawMode.Random)))
        {
            drawRulesDraft.EnsureSuccessStatusCode();
        }

        using (var prepare = await HostStageHttpHelpers.PostPrepareAsync(client, stageId))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await client.PutAsJsonAsync(
            DrawRulesUri(stageId),
            new ReplaceStageDrawRulesRequest(Clear: false, Mode: DrawMode.Random, NumberOfPots: 2),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(StageErrorCodes.StructureLocked);
    }

    private static Uri DrawRulesUri(Guid stageId) =>
        new($"/stages/{stageId}/draw-rules", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private static async Task<Guid> CreateCompetitionAsync(HttpClient client, string prefix)
    {
        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"{prefix} {Guid.CreateVersion7():N}"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        return created!.Id;
    }

    private static async Task<Guid[]> AddEntriesAsync(HttpClient client, Guid competitionId, int count)
    {
        StructureViewDto? view = null;
        for (var i = 0; i < count; i++)
        {
            using var response = await client.PostAsJsonAsync(
                $"/competitions/{competitionId}/entries",
                new AddEntryRequest($"Team {i}"));
            response.EnsureSuccessStatusCode();
            view = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        }

        return [.. view!.Participants.Entries.Select(entry => entry.EntryId)];
    }

    private static async Task SeedStageAffectationAsync(HttpClient client, Guid stageId, Guid[] entryIds)
    {
        using var affectation = await client.PutAsJsonAsync(
            $"/stages/{stageId}/affectation",
            new EntryIdsRequest(entryIds));
        affectation.EnsureSuccessStatusCode();
    }
}
