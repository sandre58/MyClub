// -----------------------------------------------------------------------
// <copyright file="CompleteStageEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompleteStageEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Complete_returns_204_and_persists_CompletedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningFinishedAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CompleteUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = await stages.GetByIdForUpdateAsync(stageId);
        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(StageStatus.Completed);
    }

    [IntegrationFact]
    public async Task Complete_when_no_matches_on_running_stage_returns_204Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningNoMatchesAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CompleteUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = await stages.GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Completed);
    }

    [IntegrationFact]
    public async Task Complete_when_missing_returns_404_ProblemDetailsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var missingId = StageId.New();

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CompleteUri(missingId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Complete_when_open_match_returns_409_with_reasonsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningScheduledAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CompleteUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageCompletionBlocked);
        GetReasons(problem!).Should().Contain(CompleteStage.ReasonOpenMatches);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = await stages.GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Running);
    }

    [IntegrationFact]
    public async Task Complete_when_Ready_returns_409_ProblemDetailsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedReadyChampionshipAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CompleteUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(StageErrorCodes.InvalidTransition);
    }

    private static Uri CompleteUri(StageId stageId) => new($"/stages/{stageId.Value}/complete", UriKind.Relative);

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private static IReadOnlyList<string> GetReasons(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("reasons", out var raw) || raw is null
            ? []
            : raw switch
            {
                JsonElement { ValueKind: JsonValueKind.Array } element => [
                    .. element.EnumerateArray().Select(item => item.GetString()!)
                ]!,
                IEnumerable<string> texts => [.. texts],
                _ => []
            };

    private async Task<StageId> SeedReadyChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Championnat Complete Ready", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }

    private async Task<StageId> SeedRunningNoMatchesAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Championnat Complete Empty", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }

    private async Task<StageId> SeedRunningFinishedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Stage Complete Done"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }

    private async Task<StageId> SeedRunningScheduledAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Stage Complete Open"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }
}
