// -----------------------------------------------------------------------
// <copyright file="ReleaseDrawAlignedPlacementsEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReleaseDrawAlignedPlacementsEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 29, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Get_stage_exposes_actions_and_release_persistsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedSlotDrawAsync(factory, cancel: true, running: false);

        using var client = factory.CreateClient();
        using var overviewResponse = await client.GetAsync(new Uri($"/stages/{seed.StageId.Value}", UriKind.Relative));
        overviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await overviewResponse.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        overview.Should().NotBeNull();
        overview!.ReleaseCandidateDrawId.Should().Be(seed.DrawId.Value);
        var draw = overview.Draws.Should().ContainSingle().Subject;
        draw.Actions.Should().NotBeNull();
        draw.Actions!.Release.State.Should().Be(StageDrawActionState.Available);
        draw.Actions.Release.ReleasableCount.Should().Be(1);

        using var release = await client.PostAsync(ReleaseUri(seed.StageId, seed.DrawId), content: null);
        release.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await release.Content.ReadFromJsonAsync<ReleaseDrawAlignedPlacementsDto>(HostJson.Options);
        body!.ReleasedCount.Should().Be(1);
        body.SkippedCount.Should().Be(0);
    }

    [IntegrationFact]
    public async Task Release_when_draw_is_not_cancelled_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedSlotDrawAsync(factory, cancel: false, running: false);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ReleaseUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawReleaseRequiresCancelled);
    }

    [IntegrationFact]
    public async Task Release_when_stage_is_running_returns_409_structure_lockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedSlotDrawAsync(factory, cancel: true, running: true);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ReleaseUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(StageErrorCodes.StructureLocked);
    }

    private static Uri ReleaseUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/release-aligned-placements", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<DrawSeed> SeedSlotDrawAsync(PlayUpWebApplicationFactory factory, bool cancel, bool running)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Release Draw"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Tour", _clock);
        stage.AddSlot("A");
        var entry = EntryId.New();
        stage.ReplaceAffectationAuthoring([entry], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        if (cancel)
        {
            stage.CancelDraw(draw.Id, _clock);
        }

        if (running)
        {
            stage.Prepare(_clock);
            stage.Start(_clock);
        }

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();
        return new DrawSeed(stage.Id, draw.Id);
    }

    private sealed record DrawSeed(StageId StageId, DrawId DrawId);
}
