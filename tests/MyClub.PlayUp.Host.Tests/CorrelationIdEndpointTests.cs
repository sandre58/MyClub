// -----------------------------------------------------------------------
// <copyright file="CorrelationIdEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.Media.Application;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CorrelationIdEndpointTests(HostPostgresFixture fixture)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    [IntegrationFact]
    public async Task Missing_competition_returns_404_with_matching_correlation_idsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.CreateVersion7()}/workspace");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var headerId = AssertCorrelationHeader(response);
        var bodyId = await ReadCorrelationIdAsync(response).ConfigureAwait(false);
        bodyId.Should().Be(headerId);
    }

    [IntegrationFact]
    public async Task Valid_inbound_correlation_id_is_echoedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        const string inbound = "client-corr-01KTESTVALID01";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/competitions/{Guid.CreateVersion7()}/workspace");
        request.Headers.TryAddWithoutValidation(CorrelationHeader, inbound);

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        AssertCorrelationHeader(response).Should().Be(inbound);
        (await ReadCorrelationIdAsync(response).ConfigureAwait(false)).Should().Be(inbound);
    }

    [IntegrationFact]
    public async Task Invalid_inbound_correlation_id_is_replacedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        const string invalid = "bad id with spaces!";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/competitions/{Guid.CreateVersion7()}/workspace");
        request.Headers.TryAddWithoutValidation(CorrelationHeader, invalid);

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var headerId = AssertCorrelationHeader(response);
        headerId.Should().NotBe(invalid);
        headerId.Should().MatchRegex("^[A-Za-z0-9._-]+$");
        (await ReadCorrelationIdAsync(response).ConfigureAwait(false)).Should().Be(headerId);
    }

    [IntegrationFact]
    public async Task Storage_delete_failure_returns_500_with_matching_correlation_idsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString, failMediaDelete: true);
        using var client = factory.CreateClient();

        var mediaId = await UploadPngAsync(client).ConfigureAwait(false);

        using var response = await client.DeleteAsync($"/media/{mediaId}");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var headerId = AssertCorrelationHeader(response);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        ReadExtensionString(problem, "correlationId").Should().Be(headerId);
        ReadExtensionString(problem, "code").Should().Be(MediaApplicationErrorCodes.StorageDeleteFailed);
    }

    private static string AssertCorrelationHeader(HttpResponseMessage response)
    {
        response.Headers.TryGetValues(CorrelationHeader, out var values).Should().BeTrue();
        var id = values!.Single();
        id.Should().NotBeNullOrWhiteSpace();
        return id;
    }

    private static async Task<string> ReadCorrelationIdAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        return ReadExtensionString(problem, "correlationId");
    }

    private static string ReadExtensionString(ProblemDetails problem, string key)
    {
        problem.Extensions.Should().ContainKey(key);
        var value = problem.Extensions[key];
        return value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString()!,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static async Task<Guid> UploadPngAsync(HttpClient client)
    {
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "corr.png");

        using var response = await client.PostAsync("/media", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var metadata = await response.Content.ReadFromJsonAsync<MediaMetadataResponse>(HostJson.Options);
        metadata.Should().NotBeNull();
        return metadata.Id;
    }
}
