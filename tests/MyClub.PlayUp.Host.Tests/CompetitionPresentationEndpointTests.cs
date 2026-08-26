// -----------------------------------------------------------------------
// <copyright file="CompetitionPresentationEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionPresentationEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Presentation_and_schedule_round_trip_on_organisation_viewAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var create = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"Meta {Guid.CreateVersion7():N}"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var summary = await create.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        summary.Should().NotBeNull();
        var id = summary.Id;

        using var presentation = await client.PostAsJsonAsync(
            $"/competitions/{id}/presentation",
            new UpdateCompetitionPresentationRequest("META", "/seed-logos/ligue-1/competition.png"));
        presentation.StatusCode.Should().Be(HttpStatusCode.OK);
        var org = await presentation.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        org.Should().NotBeNull();
        org.ShortName.Should().Be("META");
        org.LogoPath.Should().Be("/seed-logos/ligue-1/competition.png");

        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2027, 5, 30, 0, 0, 0, TimeSpan.Zero);
        using var schedule = await client.PostAsJsonAsync(
            $"/competitions/{id}/schedule",
            new SetCompetitionScheduleRequest(start, end));
        schedule.EnsureSuccessStatusCode();
        org = await schedule.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        org!.ScheduledStart.Should().Be(start);
        org.ScheduledEnd.Should().Be(end);

        using var add = await client.PostAsJsonAsync(
            $"/competitions/{id}/entries",
            new AddEntryRequest(
                "Paris Saint-Germain",
                ShortName: "PSG",
                LogoPath: "/seed-logos/ligue-1/psg.png",
                PrimaryColor: "#004170",
                SecondaryColor: "#DA291C"));
        add.EnsureSuccessStatusCode();
        org = await add.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        var entry = org!.Participants.Entries.Should().ContainSingle().Subject;
        entry.ShortName.Should().Be("PSG");
        entry.LogoPath.Should().Be("/seed-logos/ligue-1/psg.png");
        entry.PrimaryColor.Should().Be("#004170");
        entry.SecondaryColor.Should().Be("#DA291C");
    }
}
