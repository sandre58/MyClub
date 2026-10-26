// -----------------------------------------------------------------------
// <copyright file="CompetitionStructureStageOrderEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionStructureStageOrderEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Insert_positions_append_and_reorder_roundtripAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client);

        var anchor = await AppendChampionshipAsync(client, competitionId, "C");
        var first = await InsertChampionshipAsync(client, competitionId, "A", anchor.StageId, "Before");
        var middle = await InsertChampionshipAsync(client, competitionId, "B", anchor.StageId, "Before");
        var after = await InsertChampionshipAsync(client, competitionId, "D", anchor.StageId, "After");
        var appended = await AppendChampionshipAsync(client, competitionId, "E");

        first.Structure.Stages.Select(stage => stage.Name).Should().Equal("A", "C");
        middle.Structure.Stages.Select(stage => stage.Name).Should().Equal("A", "B", "C");
        after.Structure.Stages.Select(stage => stage.Name).Should().Equal("A", "B", "C", "D");
        appended.Structure.Stages.Select(stage => stage.Name).Should().Equal("A", "B", "C", "D", "E");

        var ids = appended.Structure.Stages.ToDictionary(stage => stage.Name, stage => stage.StageId);
        using var reorder = await client.PutAsJsonAsync(
            $"/competitions/{competitionId}/stages/order",
            new SetCompetitionStageOrderRequest([ids["E"], ids["A"], ids["B"], ids["C"], ids["D"]]));
        reorder.StatusCode.Should().Be(HttpStatusCode.OK);
        var reordered = await reorder.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        reordered!.Stages.Select(stage => stage.Name).Should().Equal("E", "A", "B", "C", "D");

        var reread = await GetStructureAsync(client, competitionId);
        reread.Stages.Select(stage => stage.StageId).Should().Equal(
            ids["E"],
            ids["A"],
            ids["B"],
            ids["C"],
            ids["D"]);
    }

    [IntegrationFact]
    public async Task Insert_unknown_anchor_persists_no_stageAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client);
        await AppendChampionshipAsync(client, competitionId, "A");

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages/insert",
            new InsertCompetitionStageRequest(
                "Championship",
                "Ghost",
                Guid.CreateVersion7(),
                "Before"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotInCompetition);

        var structure = await GetStructureAsync(client, competitionId);
        structure.Stages.Select(stage => stage.Name).Should().Equal("A");
    }

    [IntegrationFact]
    public async Task Insert_unknown_position_returns_invalid_intentAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client);
        var anchor = await AppendChampionshipAsync(client, competitionId, "A");

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages/insert",
            new InsertCompetitionStageRequest("Championship", "B", anchor.StageId, "Beside"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);

        var structure = await GetStructureAsync(client, competitionId);
        structure.Stages.Select(stage => stage.Name).Should().Equal("A");
    }

    [IntegrationFact]
    public async Task Insert_and_reorder_are_refused_while_runningAsync()
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

        var only = await AppendChampionshipAsync(client, competitionId, "Only");
        using (var prepare = await client.PostAsync($"/competitions/{competitionId}/prepare", content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var start = await client.PostAsync($"/competitions/{competitionId}/start", content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var insert = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages/insert",
            new InsertCompetitionStageRequest("Championship", "Late", only.StageId, "After"));
        insert.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        GetCode((await insert.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))!)
            .Should().Be(ApplicationErrorCodes.StructureNotMutable);

        using var reorder = await client.PutAsJsonAsync(
            $"/competitions/{competitionId}/stages/order",
            new SetCompetitionStageOrderRequest([only.StageId]));
        reorder.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        GetCode((await reorder.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))!)
            .Should().Be(ApplicationErrorCodes.StructureNotMutable);

        var structure = await GetStructureAsync(client, competitionId);
        structure.Stages.Select(stage => stage.Name).Should().Equal("Only");
        structure.Status.Should().Be(CompetitionStatus.Running);
    }

    [IntegrationFact]
    public async Task Reorder_leaves_qualification_intents_unchangedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client);

        using var groupsResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages",
            new AddCompetitionStageRequest("Groups", "Groups", GroupCount: 2, ParticipantsPerGroup: 2));
        groupsResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var groups = (await groupsResponse.Content.ReadFromJsonAsync<AddCompetitionStageResponse>(HostJson.Options))!;

        var final = await AppendChampionshipAsync(client, competitionId, "Final");
        var intentId = Guid.CreateVersion7();
        using var rules = await client.PutAsJsonAsync(
            $"/stages/{groups.StageId}/qualification-rules",
            new ReplaceStageQualificationRulesRequest(
            [
                new QualificationIntentRequest(
                    intentId,
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    1,
                    final.StageId,
                    DestinationForm: true)
            ]));
        rules.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var before = await GetStructureAsync(client, competitionId);
        var beforeIntents = before.Stages.Single(stage => stage.StageId == groups.StageId).QualificationIntents;

        using var reorder = await client.PutAsJsonAsync(
            $"/competitions/{competitionId}/stages/order",
            new SetCompetitionStageOrderRequest([final.StageId, groups.StageId]));
        reorder.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await GetStructureAsync(client, competitionId);
        after.Stages.Select(stage => stage.StageId).Should().Equal(final.StageId, groups.StageId);
        after.Stages.Single(stage => stage.StageId == groups.StageId).QualificationIntents
            .Should().BeEquivalentTo(beforeIntents);
        after.Stages.Single(stage => stage.StageId == groups.StageId).QualificationIntents!
            .Should().ContainSingle()
            .Which.DestinationStageId.Should().Be(final.StageId);
    }

    private static async Task<Guid> CreateCompetitionAsync(HttpClient client)
    {
        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"StageOrder {Guid.CreateVersion7():N}"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        return created!.Id;
    }

    private static async Task<AddCompetitionStageResponse> AppendChampionshipAsync(
        HttpClient client,
        Guid competitionId,
        string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages",
            new AddCompetitionStageRequest("Championship", name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AddCompetitionStageResponse>(HostJson.Options);
        return body!;
    }

    private static async Task<InsertCompetitionStageResponse> InsertChampionshipAsync(
        HttpClient client,
        Guid competitionId,
        string name,
        Guid anchorStageId,
        string position)
    {
        using var response = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/stages/insert",
            new InsertCompetitionStageRequest("Championship", name, anchorStageId, position));
        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: payload);
        return JsonSerializer.Deserialize<InsertCompetitionStageResponse>(payload, HostJson.Options)!;
    }

    private static async Task<StructureViewDto> GetStructureAsync(HttpClient client, Guid competitionId)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        return body!;
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
