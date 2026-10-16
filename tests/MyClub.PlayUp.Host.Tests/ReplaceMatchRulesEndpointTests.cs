// -----------------------------------------------------------------------
// <copyright file="ReplaceMatchRulesEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class ReplaceMatchRulesEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 9, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_when_ready_returns_409_StructureLockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedReadyChampionshipAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            RulesUri(stageId),
            new ReplaceStageMatchRulesRequest(
                DurationPerPeriod: 45,
                NumberOfPeriods: 2,
                HalfTimeDuration: 15),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(StageErrorCodes.StructureLocked);
    }

    private static Uri RulesUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/match-rules", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<StageId> SeedReadyChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName("Match rules Host League Ready"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var stage = Stage.Create(competition.Id, new StageName("Championship"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        competition.AddStage(stage.Id, _clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return stage.Id;
    }
}
