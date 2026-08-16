// -----------------------------------------------------------------------
// <copyright file="CompetitionSlice3EndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSlice3EndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Championship_materialize_then_optional_schedule_roundtripAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "Champ3");
        await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Championship", MatchdayCount: 1));
        structureResponse.EnsureSuccessStatusCode();
        var org = await structureResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        var stageId = org!.Format.PrimaryStageId!.Value;

        using var materializeResponse = await client.PostAsync(
            $"/stages/{stageId}/matches/materialize",
            null);
        materializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var orgAfter = await client.GetAsync($"/competitions/{competitionId}/organisation");
        var view = await orgAfter.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        view!.Readiness.ReadyForMatchOperation.Should().BeTrue();
        view.Readiness.AttachedMatchCount.Should().Be(6);

        var resourceId = Guid.CreateVersion7();
        var start = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 9, 30, 18, 0, 0, TimeSpan.Zero);
        using var generateResponse = await client.PostAsJsonAsync(
            $"/stages/{stageId}/schedule/generate",
            new GenerateScheduleRequest(
                start,
                end,
                GranularityMinutes: 60,
                TimeZoneId: "Europe/Paris",
                ResourceIds: [resourceId],
                MatchDurationMinutes: 90));
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var proposal = await generateResponse.Content.ReadFromJsonAsync<ScheduleProposalDto>(HostJson.Options);
        proposal.Should().NotBeNull();
        if (proposal.IsSuccess)
        {
            using var applyResponse = await client.PostAsJsonAsync(
                $"/stages/{stageId}/schedule/apply",
                new ApplyScheduleRequest(
                    proposal.Assignments,
                    [.. proposal.Assignments.Select(a => a.MatchId)]));
            applyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    [IntegrationFact]
    public async Task Groups_draw_pipeline_and_materialize_persistsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "Groups3");
        await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Groups", GroupCount: 2, ParticipantsPerGroup: 2));
        var org = await structureResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        var stageId = org!.Format.PrimaryStageId!.Value;

        using var createDraw = await client.PostAsJsonAsync(
            $"/stages/{stageId}/draws",
            new CreateDrawRequest("Group"));
        createDraw.StatusCode.Should().Be(HttpStatusCode.Created);
        var draw = await createDraw.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);
        draw!.Kind.Should().Be(DrawResolutionKind.Group);

        using var inputs = await client.PostAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/inputs",
            null);
        inputs.EnsureSuccessStatusCode();

        using var generate = await client.PostAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/generate",
            null);
        generate.EnsureSuccessStatusCode();
        var generated = await generate.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options);
        generated!.IsResolved.Should().BeTrue();

        using var publish = await client.PostAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/publish",
            null);
        publish.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var apply = await client.PostAsJsonAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/apply",
            new ApplyDrawRequest());
        apply.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var materialize = await client.PostAsync(
            $"/stages/{stageId}/matches/materialize",
            null);
        materialize.EnsureSuccessStatusCode();

        using var orgAfter = await client.GetAsync($"/competitions/{competitionId}/organisation");
        var view = await orgAfter.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        view!.Readiness.ReadyForMatchOperation.Should().BeTrue();
        view.Readiness.AttachedMatchCount.Should().Be(2);
    }

    [IntegrationFact]
    public async Task Cup_pairing_draw_apply_creates_matchesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "Cup3");
        await AddEntriesAsync(client, competitionId, 4);

        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/organisation/structure",
            new ConfigureStructureRequest("Cup", BracketSize: 4));
        var org = await structureResponse.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        var stageId = org!.Format.PrimaryStageId!.Value;

        using var createDraw = await client.PostAsJsonAsync(
            $"/stages/{stageId}/draws",
            new CreateDrawRequest("Pairing"));
        var draw = await createDraw.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);

        await client.PostAsync($"/stages/{stageId}/draws/{draw!.DrawId}/inputs", null);
        using var generate = await client.PostAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/generate",
            null);
        (await generate.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options))!.IsResolved.Should().BeTrue();

        await client.PostAsync($"/stages/{stageId}/draws/{draw.DrawId}/publish", null);
        using var apply = await client.PostAsJsonAsync(
            $"/stages/{stageId}/draws/{draw.DrawId}/apply",
            new ApplyDrawRequest());
        apply.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var orgAfter = await client.GetAsync($"/competitions/{competitionId}/organisation");
        var view = await orgAfter.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        view!.Readiness.ReadyForMatchOperation.Should().BeTrue();
        view.Readiness.AttachedMatchCount.Should().Be(2);
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
