// -----------------------------------------------------------------------
// <copyright file="StageRevertResumeEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class StageRevertResumeEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Revert_to_ready_from_Running_persists_ReadyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningWithScheduledMatchesAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RevertToReadyUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Ready);
    }

    [IntegrationFact]
    public async Task Resume_from_Suspended_persists_RunningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedSuspendedAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ResumeUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Running);
    }

    private static Uri RevertToReadyUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/revert-to-ready", UriKind.Relative);

    private static Uri ResumeUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/resume", UriKind.Relative);

    private async Task<StageId> SeedRunningWithScheduledMatchesAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Revert Ready Host", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            situation.TrackMatch(match);
        }

        stage.Start(_clock);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return stage.Id;
    }

    private async Task<StageId> SeedSuspendedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Resume Host", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            situation.TrackMatch(match);
        }

        stage.Start(_clock);
        stage.Suspend(_clock);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return stage.Id;
    }
}
