// -----------------------------------------------------------------------
// <copyright file="ReplaceStandingRulesEndpointTests.cs" company="Stéphane ANDRE">
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
public sealed class ReplaceStandingRulesEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 9, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_standing_rules_returns_204_and_persists_while_runningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningChampionshipAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            RulesUri(stageId),
            new ReplaceStageStandingRulesRequest(
                WinPoints: 2,
                DrawPoints: 1,
                LossPoints: 0,
                RankingCriteria: [RankingCriterion.Points, RankingCriterion.Wins]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
        stage.Should().NotBeNull();
        stage.Status.Should().Be(StageStatus.Running);
        stage.Regulation.StandingRules.Should().NotBeNull();
        stage.Regulation.StandingRules!.Points.WinPoints.Should().Be(2);
        stage.Regulation.StandingRules.RankingCriteria.Should().Equal(
            RankingCriterion.Points,
            RankingCriterion.Wins);
    }

    [IntegrationFact]
    public async Task Put_on_cup_returns_domain_problemAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedCupStageAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            RulesUri(stageId),
            new ReplaceStageStandingRulesRequest(
                3,
                1,
                0,
                [RankingCriterion.Points]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(StageErrorCodes.StandingRulesInvariant);
    }

    [IntegrationFact]
    public async Task Put_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            RulesUri(StageId.New()),
            new ReplaceStageStandingRulesRequest(
                3,
                1,
                0,
                [RankingCriterion.Points]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    private static Uri RulesUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/standing-rules", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<StageId> SeedRunningChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName("Standing Host League"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var stage = Stage.Create(competition.Id, new StageName("Championship"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.AddStage(stage.Id, _clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return stage.Id;
    }

    private async Task<StageId> SeedCupStageAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName("Standing Host Cup"),
            SampleRegulations.Standard(),
            _clock);

        var stage = Stage.Create(
            competition.Id,
            new StageName("KO"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: false),
            _clock);
        stage.AddRound("Final", _clock);
        competition.AddStage(stage.Id, _clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return stage.Id;
    }
}
