// -----------------------------------------------------------------------
// <copyright file="CompetitionOrganisationEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionOrganisationEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Entries_structure_organisation_roundtrip_persistsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"Slice2 Org {Guid.CreateVersion7():N}"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        created.Should().NotBeNull();
        var competitionId = created.Id;

        using var addResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries",
            new AddEntryRequest("Alpha"));
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAdd = await addResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        afterAdd.Should().NotBeNull();
        afterAdd.Participants.ActiveCount.Should().Be(1);

        using var addSecond = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries",
            new AddEntryRequest("Beta"));
        addSecond.StatusCode.Should().Be(HttpStatusCode.OK);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Championship", MatchdayCount: 2));
        structureResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var structured = await structureResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        structured.Should().NotBeNull();
        structured.Format.Kind.Should().Be(StructureFormatKind.Championship);
        structured.Structure.MatchdayCount.Should().Be(2);
        structured.Readiness.ReadyForNextSlice.Should().BeTrue();
        structured.Readiness.ReadyForSchedulePath.Should().BeTrue();

        using var getResponse = await client.GetAsync($"/competitions/{competitionId}/organisation");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = await getResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        reloaded.Should().NotBeNull();
        reloaded.Participants.ActiveCount.Should().Be(2);
        reloaded.Structure.MatchdayCount.Should().Be(2);
        reloaded.Format.PrimaryStageId.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task ConfigureStructure_groups_and_cup_workAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "Groups");

        await client.PostAsJsonAsync($"/competitions/{competitionId}/entries", new AddEntryRequest("A"));
        await client.PostAsJsonAsync($"/competitions/{competitionId}/entries", new AddEntryRequest("B"));

        using var groupsResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Groups", GroupCount: 2, ParticipantsPerGroup: 2));
        groupsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await groupsResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        groups!.Structure.GroupCount.Should().Be(2);
        groups.Structure.NumberOfPots.Should().Be(2);
        groups.Readiness.ReadyForDraw.Should().BeTrue();

        using var cupResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Cup", BracketSize: 4));
        cupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cup = await cupResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        cup!.Format.Kind.Should().Be(StructureFormatKind.Cup);
        cup.Structure.SlotCount.Should().Be(4);
        cup.Structure.RoundCount.Should().Be(1);
        cup.Readiness.ReadyForDraw.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Rename_withdraw_exclude_and_replace_regulationAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client, "Mutations");

        using var addResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries",
            new AddEntryRequest("Old Name"));
        var view = await addResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        var entryId = view!.Participants.Entries[0].EntryId;

        using var renameResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries/{entryId}/rename",
            new RenameEntryRequest("New Name"));
        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await renameResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options))!
            .Participants.Entries[0].DisplayName.Should().Be("New Name");

        using var addOther = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries",
            new AddEntryRequest("Other"));
        var otherId = (await addOther.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options))!
            .Participants.Entries.Single(entry => entry.DisplayName == "Other").EntryId;

        using var excludeResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries/{otherId}/exclude",
            new { });
        excludeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var withdrawResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/entries/{entryId}/withdraw",
            new { });
        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var regulationResponse = await client.PutAsJsonAsync(
            $"/competitions/{competitionId}/regulation",
            new ReplaceRegulationRequest(
                MinimumTeams: 3,
                MaximumTeams: 32,
                DurationPerPeriod: 40,
                NumberOfPeriods: 2,
                HalfTimeDuration: 10,
                WinPoints: 3,
                DrawPoints: 1,
                LossPoints: 0));
        regulationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regulated = await regulationResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        regulated!.Regulation.MinimumTeams.Should().Be(3);
        regulated.Regulation.MaximumTeams.Should().Be(32);
        regulated.Regulation.DurationPerPeriod.Should().Be(40);
    }

    [IntegrationFact]
    public async Task Cup_non_power_of_two_returns_application_errorAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var competitionId = await CreateCompetitionAsync(client, "BadCup");

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Cup", BracketSize: 6));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CupBracketNotPowerOfTwo);
    }

    [IntegrationFact]
    public async Task Organisation_unknown_competition_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.CreateVersion7()}/organisation");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
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
