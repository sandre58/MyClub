// -----------------------------------------------------------------------
// <copyright file="DematerializeEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class DematerializeEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Championship_materialize_then_dematerialize_returns_contract_and_clears_attachmentsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "DematChamp");
        await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Championship"));
        structureResponse.EnsureSuccessStatusCode();
        var stageId = (await structureResponse.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
            .Structure.Format.PrimaryStageId!.Value;

        using (var materialize = await client.PostAsync($"/stages/{stageId}/matches/materialize", null))
        {
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var structureBefore = await client.GetAsync($"/competitions/{competitionId}/structure");
        var before = await structureBefore.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        before!.Readiness.AttachedMatchCount.Should().Be(6);

        using var dematerialize = await client.PostAsync($"/stages/{stageId}/matches/dematerialize", null);
        dematerialize.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await dematerialize.Content.ReadFromJsonAsync<DematerializePhaseResponse>(HostJson.Options);
        body!.RemovedMatchCount.Should().Be(6);
        body.RemovedRoundNumber.Should().BeNull();

        using var structureAfter = await client.GetAsync($"/competitions/{competitionId}/structure");
        var after = await structureAfter.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        after!.Readiness.AttachedMatchCount.Should().Be(0);
    }

    [IntegrationFact]
    public async Task Cup_materialize_from_slots_then_dematerialize_returns_contractAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "DematCup");
        var entryIds = await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Cup", BracketSize: 4));
        structureResponse.EnsureSuccessStatusCode();
        var stageId = (await structureResponse.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
            .Structure.Format.PrimaryStageId!.Value;

        await SeedStageAffectationAsync(client, stageId, entryIds);

        using (var drawRules = await client.PutAsJsonAsync(
                   $"/stages/{stageId}/draw-rules",
                   new ReplaceStageDrawRulesRequest(Clear: false, Mode: DrawMode.Random)))
        {
            drawRules.EnsureSuccessStatusCode();
        }

        using var createDraw = await client.PostAsJsonAsync(
            $"/stages/{stageId}/draws",
            new CreateDrawRequest("Slot"));
        createDraw.EnsureSuccessStatusCode();
        var draw = await createDraw.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);

        using (var inputs = await client.PostAsync($"/stages/{stageId}/draws/{draw!.DrawId}/inputs", null))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(
                   $"/stages/{stageId}/draws/{draw.DrawId}/generate",
                   null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using (var publish = await client.PostAsync($"/stages/{stageId}/draws/{draw.DrawId}/publish", null))
        {
            publish.EnsureSuccessStatusCode();
        }

        using (var apply = await client.PostAsync($"/stages/{stageId}/draws/{draw.DrawId}/apply", null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var materialize = await client.PostAsJsonAsync(
                   $"/stages/{stageId}/matches/materialize-from-slots",
                   new MaterializeCupFromOccupiedSlotsRequest(["P1", "P2"])))
        {
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var dematerialize = await client.PostAsync($"/stages/{stageId}/matches/dematerialize", null);
        dematerialize.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await dematerialize.Content.ReadFromJsonAsync<DematerializePhaseResponse>(HostJson.Options);
        body!.RemovedMatchCount.Should().Be(2);
        body.RemovedRoundNumber.Should().BeNull();

        using var structureAfter = await client.GetAsync($"/competitions/{competitionId}/structure");
        var after = await structureAfter.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        after!.Readiness.AttachedMatchCount.Should().Be(0);
    }

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
