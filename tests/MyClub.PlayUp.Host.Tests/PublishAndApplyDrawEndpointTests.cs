// -----------------------------------------------------------------------
// <copyright file="PublishAndApplyDrawEndpointTests.cs" company="Stéphane ANDRE">
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
public sealed class PublishAndApplyDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 20, 45, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task PublishAndApply_returns_204_and_persists_Published_with_slot_appliedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedResolvedDraftSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(PublishAndApplyUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
        stage.Slots.Should().ContainSingle(slot => slot.SlotKey == "A" && slot.EntryId == seed.EntryId);
    }

    [IntegrationFact]
    public async Task PublishAndApply_when_stage_missing_returns_404_StageNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            PublishAndApplyUri(StageId.New(), DrawId.New()),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    private static Uri PublishAndApplyUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish-and-apply", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<DrawSeed> SeedResolvedDraftSlotDrawAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName("PublishAndApply Cup"),
            SampleRegulations.Standard(),
            _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new DrawSeed(stage.Id, draw.Id, entry);
    }

    private sealed record DrawSeed(StageId StageId, DrawId DrawId, EntryId EntryId);
}
