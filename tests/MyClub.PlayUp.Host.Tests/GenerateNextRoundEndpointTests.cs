// -----------------------------------------------------------------------
// <copyright file="GenerateNextRoundEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class GenerateNextRoundEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 26, 15, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Generate_next_round_while_running_creates_matchesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningSwissAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(GenerateNextRoundUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GenerateNextRoundResponse>(HostJson.Options);
        body.Should().NotBeNull();
        body!.AlreadyComplete.Should().BeFalse();
        body.RoundIndex.Should().Be(1);
        body.CreatedCount.Should().Be(2);
        body.AttachedMatchIds.Should().HaveCount(2);

        using var scope = factory.Services.CreateScope();
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stageId);
        matches.Should().HaveCount(2);
    }

    private static Uri GenerateNextRoundUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/swiss/generate-next-round", UriKind.Relative);

    private async Task<StageId> SeedRunningSwissAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("Swiss Host", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = Stage.Create(competition.Id, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(3));
        competition.AddStage(stage.Id, _clock);
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }
}
