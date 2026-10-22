// -----------------------------------------------------------------------
// <copyright file="HostExposureWarningIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class HostExposureWarningIntegrationTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task T3_host_starts_and_serves_api_with_default_test_factoryAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/competitions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [IntegrationFact]
    public async Task T4_development_rejects_disallowed_host_headerAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/competitions");
        request.Headers.TryAddWithoutValidation("Host", "evil.example");

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task T5_production_keeps_allowed_hosts_wildcardAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString, environment: "Production");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/competitions");
        request.Headers.TryAddWithoutValidation("Host", "arbitrary.example");

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
