// -----------------------------------------------------------------------
// <copyright file="RevertStageEndpointTests.cs" company="Stéphane ANDRE">
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
public sealed class RevertStageEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Revert_to_draft_from_Ready_persists_DraftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedReadyWithoutMatchesAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RevertToDraftUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Draft);
    }

    [IntegrationFact]
    public async Task Suspend_from_Running_keeps_attached_matchesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (stageId, matchCount) = await SeedRunningWithMaterializedMatchesAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(SuspendUri(stageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
        loaded!.Status.Should().Be(StageStatus.Suspended);
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
        stageMatches.Should().HaveCount(matchCount);
    }

    private static Uri RevertToDraftUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/revert-to-draft", UriKind.Relative);

    private static Uri SuspendUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/suspend", UriKind.Relative);

    private async Task<StageId> SeedReadyWithoutMatchesAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Revert Draft Host", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.RequirePrimaryStage().Id;
    }

    private async Task<(StageId StageId, int MatchCount)> SeedRunningWithMaterializedMatchesAsync(
        PlayUpWebApplicationFactory factory)
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
        return (stage.Id, materialized.CreatedMatches.Count);
    }
}
