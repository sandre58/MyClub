// -----------------------------------------------------------------------
// <copyright file="CompetitionPresentationEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
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
    public async Task Presentation_and_schedule_round_trip_on_structure_viewAsync()
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

        var competitionLogoId = await UploadPngAsync(client, "competition.png");
        var entryLogoId = await UploadPngAsync(client, "psg.png");

        using var presentation = await client.PostAsJsonAsync(
            $"/competitions/{id}/presentation",
            new UpdateCompetitionPresentationRequest("META", competitionLogoId));
        presentation.StatusCode.Should().Be(HttpStatusCode.OK);
        var org = await presentation.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        org.Should().NotBeNull();
        org.ShortName.Should().Be("META");
        org.LogoMediaId.Should().Be(competitionLogoId);

        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2027, 5, 30, 0, 0, 0, TimeSpan.Zero);
        using var schedule = await client.PostAsJsonAsync(
            $"/competitions/{id}/schedule",
            new SetCompetitionScheduleRequest(start, end));
        schedule.EnsureSuccessStatusCode();
        org = await schedule.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        org!.ScheduledStart.Should().Be(start);
        org.ScheduledEnd.Should().Be(end);

        using var listResponse = await client.GetAsync("/competitions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<CompetitionListItemDto>>(HostJson.Options);
        var row = list.Should().Contain(item => item.Id == id).Which;
        row.ScheduledStart.Should().Be(start);
        row.ScheduledEnd.Should().Be(end);
        row.LogoMediaId.Should().Be(competitionLogoId);

        using var add = await client.PostAsJsonAsync(
            $"/competitions/{id}/entries",
            new AddEntryRequest(
                "Paris Saint-Germain",
                ShortName: "PSG",
                LogoMediaId: entryLogoId,
                PrimaryColor: "#004170",
                SecondaryColor: "#DA291C"));
        add.EnsureSuccessStatusCode();
        org = await add.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        var entry = org!.Participants.Entries.Should().ContainSingle().Subject;
        entry.ShortName.Should().Be("PSG");
        entry.LogoMediaId.Should().Be(entryLogoId);
        entry.PrimaryColor.Should().Be("#004170");
        entry.SecondaryColor.Should().Be("#DA291C");

        using var content = await client.GetAsync($"/media/{competitionLogoId}/content");
        content.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        content.Headers.TryGetValues("Cache-Control", out var cacheControlValues).Should().BeTrue();
        cacheControlValues!.Single().Should().Be("public, max-age=31536000, immutable");
        var body = await content.Content.ReadAsByteArrayAsync();
        body.Should().Equal(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
    }

    private static async Task<Guid> UploadPngAsync(HttpClient client, string fileName)
    {
        // Minimal valid 1×1 PNG
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", fileName);

        using var response = await client.PostAsync("/media", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metadata = await response.Content.ReadFromJsonAsync<MediaMetadataResponse>(HostJson.Options);
        metadata.Should().NotBeNull();
        return metadata.Id;
    }
}
