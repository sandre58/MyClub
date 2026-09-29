// -----------------------------------------------------------------------
// <copyright file="HttpContractEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Minimal Phase 12.8 HTTP contract tests (string enums + named response DTOs).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class HttpContractEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Workspace_status_is_serialized_as_json_string_not_numberAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"Contract {Guid.CreateVersion7():N}"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await createResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var status = document.RootElement.GetProperty("status");
        status.ValueKind.Should().Be(JsonValueKind.String);
        status.GetString().Should().Be("Draft");
    }

    [IntegrationFact]
    public async Task Materialize_returns_named_contract_shapeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "MatContract");
        await AddEntriesAsync(client, competitionId, 4);
        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Championship"));
        structureResponse.EnsureSuccessStatusCode();
        var org = (await structureResponse.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
            .Structure;
        var stageId = org.Format.PrimaryStageId!.Value;

        using var materializeResponse = await client.PostAsync(
            $"/stages/{stageId}/matches/materialize",
            content: null);
        materializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await materializeResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("createdCount").GetInt32().Should().Be(6);
        root.GetProperty("alreadyComplete").GetBoolean().Should().BeFalse();
        root.GetProperty("attachedMatchIds").GetArrayLength().Should().Be(6);

        var typed = JsonSerializer.Deserialize<MaterializeMatchesResponse>(json, HostJson.Options);
        typed.Should().NotBeNull();
        typed.CreatedCount.Should().Be(6);
        typed.AttachedMatchIds.Should().HaveCount(6);
        typed.AlreadyComplete.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Apply_qualification_returns_named_contract_shapeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (leagueId, _) = await QualificationContractSeed.CreateAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/stages/{leagueId}/qualification/apply",
            content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("appliedCount").GetInt32().Should().BeGreaterThan(0);
        var assignments = root.GetProperty("assignments");
        assignments.GetArrayLength().Should().BeGreaterThan(0);
        var first = assignments[0];
        first.TryGetProperty("stageId", out _).Should().BeTrue();
        first.TryGetProperty("entryId", out _).Should().BeTrue();
        first.TryGetProperty("slotKey", out _).Should().BeFalse();

        var typed = JsonSerializer.Deserialize<QualificationApplyResponse>(json, HostJson.Options);
        typed.Should().NotBeNull();
        typed.AppliedCount.Should().Be(typed.Assignments.Count);
        typed.Assignments.Should().Contain(a => a.EntryId != Guid.Empty);
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

    private static async Task AddEntriesAsync(HttpClient client, Guid competitionId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            using var response = await client.PostAsJsonAsync(
                $"/competitions/{competitionId}/entries",
                new AddEntryRequest($"Team {i}"));
            response.EnsureSuccessStatusCode();
        }
    }
}
