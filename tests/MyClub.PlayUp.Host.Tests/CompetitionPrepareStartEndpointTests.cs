// -----------------------------------------------------------------------
// <copyright file="CompetitionPrepareStartEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionPrepareStartEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Prepare_returns_204_and_persists_ReadyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftReadyToPrepareAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(PrepareUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdForUpdateAsync(competitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Ready);
    }

    [IntegrationFact]
    public async Task Prepare_when_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(PrepareUri(CompetitionId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task Prepare_when_already_Ready_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftReadyToPrepareAsync(factory);
        using var client = factory.CreateClient();

        using var first = await client.PostAsync(PrepareUri(competitionId), content: null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var second = await client.PostAsync(PrepareUri(competitionId), content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Prepare_without_stage_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftWithoutStageAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(PrepareUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Prepare_without_active_entry_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftWithoutEntryAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(PrepareUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Start_returns_204_and_persists_RunningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedReadyAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(StartUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdForUpdateAsync(competitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Running);
    }

    [IntegrationFact]
    public async Task Start_when_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(StartUri(CompetitionId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task Start_from_draft_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftReadyToPrepareAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(StartUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Start_when_completed_returns_409_competition_closedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedCompletedAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(StartUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionClosed);
    }

    [IntegrationFact]
    public async Task Prepare_then_start_persists_RunningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedDraftReadyToPrepareAsync(factory);
        using var client = factory.CreateClient();

        using var prepare = await client.PostAsync(PrepareUri(competitionId), content: null);
        prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var start = await client.PostAsync(StartUri(competitionId), content: null);
        start.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdForUpdateAsync(competitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Running);
    }

    private static Uri PrepareUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/prepare", UriKind.Relative);

    private static Uri StartUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/start", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<CompetitionId> SeedDraftReadyToPrepareAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Prepare Cup", _clock)
            .WithTeams("Alpha")
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<CompetitionId> SeedDraftWithoutStageAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("No Stage Cup", _clock)
            .WithTeams("Alpha");
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<CompetitionId> SeedDraftWithoutEntryAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("No Entry Cup", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<CompetitionId> SeedReadyAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Ready Cup", _clock)
            .WithTeams("Alpha")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PrepareCompetition();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<CompetitionId> SeedCompletedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Closed Cup", _clock)
            .WithTeams("Alpha")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PrepareCompetition()
            .StartCompetition()
            .CompleteCompetition(CompletionMode.Administrative);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }
}
