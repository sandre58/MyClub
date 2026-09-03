// -----------------------------------------------------------------------
// <copyright file="PublishDrawEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class PublishDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 20, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Publish_returns_204_and_persists_PublishedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedResolvedDraftDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(PublishUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
        stage.GetDraw(seed.DrawId).Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [IntegrationFact]
    public async Task Publish_when_stage_missing_returns_404_StageNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(PublishUri(StageId.New(), DrawId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Publish_when_not_resolved_returns_409_DrawInvalidTransitionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedUnresolvedDraftDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(PublishUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(StageErrorCodes.DrawInvalidTransition);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Draft);
    }

    private static Uri PublishUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private Task<DrawSeed> SeedResolvedDraftDrawAsync(PlayUpWebApplicationFactory factory) =>
        SeedDrawAsync(factory, resolved: true);

    private Task<DrawSeed> SeedUnresolvedDraftDrawAsync(PlayUpWebApplicationFactory factory) =>
        SeedDrawAsync(factory, resolved: false);

    private async Task<DrawSeed> SeedDrawAsync(PlayUpWebApplicationFactory factory, bool resolved)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Publish Draw Cup"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A", _clock);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        if (resolved)
        {
            stage.RecordDrawResolution(
                draw.Id,
                DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
                _clock);
        }

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new DrawSeed(stage.Id, draw.Id);
    }

    private sealed record DrawSeed(StageId StageId, DrawId DrawId);
}
