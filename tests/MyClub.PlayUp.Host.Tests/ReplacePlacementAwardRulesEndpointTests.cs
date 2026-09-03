// -----------------------------------------------------------------------
// <copyright file="ReplacePlacementAwardRulesEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplacePlacementAwardRulesEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_placement_award_rules_returns_204_and_persistsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinalStageAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            RulesUri(seed.StageId),
            new ReplaceStagePlacementAwardRulesRequest(
            [
                new PlacementAwardPathRequest(seed.FixtureId.Value, ProgressionOutcome.Winner, 1),
                new PlacementAwardPathRequest(seed.FixtureId.Value, ProgressionOutcome.Loser, 2)
            ]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.Regulation.PlacementAwardRules.Should().NotBeNull();
        stage.Regulation.PlacementAwardRules!.Paths.Should().HaveCount(2);
        stage.FindSlot("Unused")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Put_empty_paths_clears_rulesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinalStageAsync(factory);

        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            RulesUri(seed.StageId),
            new ReplaceStagePlacementAwardRulesRequest(
            [
                new PlacementAwardPathRequest(seed.FixtureId.Value, ProgressionOutcome.Winner, 1)
            ]),
            HostJson.Options);

        using var clear = await client.PutAsJsonAsync(
            RulesUri(seed.StageId),
            new ReplaceStagePlacementAwardRulesRequest([]),
            HostJson.Options);

        clear.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage!.Regulation.PlacementAwardRules.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Put_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            RulesUri(StageId.New()),
            new ReplaceStagePlacementAwardRulesRequest(
            [
                new PlacementAwardPathRequest(Guid.NewGuid(), ProgressionOutcome.Winner, 1)
            ]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    private static Uri RulesUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/placement-award-rules", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<(StageId StageId, FixtureId FixtureId)> SeedFinalStageAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName("Awards Host Cup"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        var addFixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("Unused", _clock);
        competition.AddStage(stage.Id, _clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return (stage.Id, addFixture.Id);
    }
}
