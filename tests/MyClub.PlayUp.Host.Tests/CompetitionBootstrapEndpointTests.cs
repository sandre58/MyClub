// -----------------------------------------------------------------------
// <copyright file="CompetitionBootstrapEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionBootstrapEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Create_list_open_workspace_roundtripAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var name = $"Slice1 Cup {Guid.CreateVersion7():N}";

        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest(name));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        created.Should().NotBeNull();
        created.Name.Should().Be(name);
        created.Status.Should().Be(CompetitionStatus.Draft);
        created.NextActionCode.Should().Be(WorkspaceSummaryAssembler.ContinueOrganisationCode);
        created.AttentionCount.Should().Be(0);
        createResponse.Headers.Location.Should().NotBeNull();
        createResponse.Headers.Location!.ToString()
            .Should().Be($"/competitions/{created.Id}/workspace");

        using var listResponse = await client.GetAsync("/competitions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<CompetitionListItemDto>>(HostJson.Options);
        list.Should().NotBeNull();
        list.Should().Contain(item => item.Id == created.Id && item.Name == name && item.Status == CompetitionStatus.Draft);

        using var workspaceResponse = await client.GetAsync($"/competitions/{created.Id}/workspace");
        workspaceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        workspace.Should().NotBeNull();
        workspace.Id.Should().Be(created.Id);
        workspace.Name.Should().Be(name);
        workspace.Status.Should().Be(CompetitionStatus.Draft);
        workspace.NextActionLabel.Should().Be("Continuer la préparation");
    }

    [IntegrationFact]
    public async Task Get_workspace_unknown_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.CreateVersion7()}/workspace");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task Create_empty_name_returns_domain_errorAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest("  "));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
