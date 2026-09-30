// -----------------------------------------------------------------------
// <copyright file="ApplyDrawEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ApplyDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 20, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Apply_slot_returns_204_and_persists_slot_occupancyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPublishedSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ApplyUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.FindSlot("S1")!.EntryId.Should().Be(seed.EntryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(seed.EntryB);
        stage.FindFixtureByBracketPairKey("P1").Should().BeNull();
        stage.Rounds[0].Fixtures.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Apply_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            ApplyUri(StageId.New(), DrawId.New()),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Apply_when_draw_draft_returns_400_and_occupies_no_slotsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedDraftSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ApplyUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.DrawApplyFailure);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage!.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Apply_materialize_then_start_persists_Live_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPublishedSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using (var apply = await client.PostAsync(ApplyUri(seed.StageId, seed.DrawId), content: null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var materialize = await client.PostAsJsonAsync(
                   MaterializeUri(seed.StageId),
                   new MaterializeCupFromOccupiedSlotsRequest(["P1"])))
        {
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
            matchId = stage!.FindFixtureByBracketPairKey("P1")!.MatchIds.Should().ContainSingle().Subject;
        }

        using (var start = await client.PostAsync(new Uri($"/matches/{matchId.Value}/start", UriKind.Relative), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
            match!.Status.Should().Be(MatchStatus.Live);
        }
    }

    private static Uri ApplyUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

    private static Uri MaterializeUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/matches/materialize-from-slots", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private Task<SlotDrawSeed> SeedPublishedSlotDrawAsync(PlayUpWebApplicationFactory factory) =>
        SeedSlotDrawAsync(factory, publish: true);

    private Task<SlotDrawSeed> SeedDraftSlotDrawAsync(PlayUpWebApplicationFactory factory) =>
        SeedSlotDrawAsync(factory, publish: false);

    private async Task<SlotDrawSeed> SeedSlotDrawAsync(PlayUpWebApplicationFactory factory, bool publish)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Apply Draw Cup"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entryA, entryB]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(entryA, "S1"),
                new SlotDrawPlacement(entryB, "S2")
            ]),
            _clock);
        if (publish)
        {
            stage.PublishDraw(draw.Id, _clock);
        }

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new SlotDrawSeed(stage.Id, draw.Id, entryA, entryB);
    }

    private sealed record SlotDrawSeed(
        StageId StageId,
        DrawId DrawId,
        EntryId EntryA,
        EntryId EntryB);
}
