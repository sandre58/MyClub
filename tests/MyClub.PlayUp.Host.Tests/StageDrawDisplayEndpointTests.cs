// -----------------------------------------------------------------------
// <copyright file="StageDrawDisplayEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Tirage read model on <c>GET /stages/{id}</c>, written against main after #151.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class StageDrawDisplayEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Get_stage_exposes_partial_draw_intent_origin_metrics_and_cellsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupAsync(factory);
        using var client = factory.CreateClient();
        var drawId = await CreatePartialDrawAsync(client, seed.StageId);

        using (var inputs = await client.PostAsJsonAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/inputs",
            new ConfigureDrawInputsRequest("Partial")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/generate",
            content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using var response = await client.GetAsync($"/stages/{seed.StageId.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var metrics = root.GetProperty("slotDrawDisplayMetrics");
        metrics.GetProperty("unplacedEntryCount").GetInt32().Should().Be(3);
        metrics.GetProperty("vacantSlotKeyCount").GetInt32().Should().Be(3);
        metrics.GetProperty("alreadyPlacedEntryCount").GetInt32().Should().Be(1);
        metrics.GetProperty("entryCount").GetInt32().Should().Be(4);
        metrics.GetProperty("placeCount").GetInt32().Should().Be(4);
        metrics.GetProperty("vacantSlotKeys").EnumerateArray().Select(key => key.GetString())
            .Should().Equal("S2", "S3", "S4");

        var draw = root.GetProperty("draws").EnumerateArray().Should().ContainSingle().Subject;
        draw.GetProperty("inputsIntent").GetString().Should().Be(nameof(DrawInputsIntent.Partial));
        draw.GetProperty("displayMetrics").GetProperty("vacantSlotKeyCount").GetInt32().Should().Be(3);
        draw.GetProperty("slotPlacements").EnumerateArray().Should().OnlyContain(placement =>
            placement.GetProperty("origin").GetString() == nameof(DrawPlacementOrigin.Draw)
            && placement.GetProperty("slotKey").GetString() != "S1");

        var cells = draw.GetProperty("slotDisplayCells").EnumerateArray().ToArray();
        cells.Should().HaveCount(4);
        var preserved = cells.Should().ContainSingle(cell => cell.GetProperty("slotKey").GetString() == "S1").Subject;
        preserved.GetProperty("cellState").GetString().Should().Be(nameof(SlotDrawCellState.AlreadyPlaced));
        preserved.GetProperty("origin").GetString().Should().Be(nameof(DrawPlacementOrigin.Manual));
        preserved.GetProperty("entryId").GetGuid().Should().Be(seed.Entries[0].Value);
        cells.Where(cell => cell.GetProperty("slotKey").GetString() != "S1").Should().OnlyContain(cell =>
            cell.GetProperty("cellState").GetString() == nameof(SlotDrawCellState.Drawn)
            && cell.GetProperty("origin").GetString() == nameof(DrawPlacementOrigin.Draw));
    }

    [IntegrationFact]
    public async Task Release_keeps_the_manual_occupant_and_the_historical_cellsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupAsync(factory);
        using var client = factory.CreateClient();
        var drawId = await CreatePartialDrawAsync(client, seed.StageId);

        using (var inputs = await client.PostAsJsonAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/inputs",
            new ConfigureDrawInputsRequest("Partial")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/generate",
            content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using (var apply = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/publish-and-apply",
            content: null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var published = await ReadCellsAsync(client, seed.StageId);
        published.Should().Contain(cell => cell.CellState == SlotDrawCellState.OutsideDraw && cell.SlotKey == "S1");
        published.Where(cell => cell.SlotKey != "S1").Should().OnlyContain(cell =>
            cell.CellState == SlotDrawCellState.Drawn && cell.Origin == DrawPlacementOrigin.Draw);

        using (var cancel = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/cancel",
            content: null))
        {
            cancel.EnsureSuccessStatusCode();
        }

        var cancelled = await ReadCellsAsync(client, seed.StageId);
        cancelled.Should().BeEquivalentTo(published);

        using var release = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/release-aligned-placements",
            content: null);
        release.EnsureSuccessStatusCode();

        (await ReadCellsAsync(client, seed.StageId)).Should().BeEquivalentTo(published);

        using var after = await client.GetAsync($"/stages/{seed.StageId.Value}");
        var overview = await after.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        overview!.Slots.Should().ContainSingle(slot => slot.SlotKey == "S1" && slot.EntryId == seed.Entries[0].Value);
        overview.Slots.Where(slot => slot.SlotKey != "S1").Should().OnlyContain(slot => slot.EntryId == null);
        overview.Draws.Should().ContainSingle()
            .Which.DisplayMetrics.Should().BeNull();
    }

    private static async Task<IReadOnlyList<StageDrawSlotDisplayCellDto>> ReadCellsAsync(
        HttpClient client,
        StageId stageId)
    {
        using var response = await client.GetAsync($"/stages/{stageId.Value}");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        var draw = overview!.Draws.Should().ContainSingle().Subject;
        draw.DisplayMetrics.Should().BeNull();
        draw.SlotDisplayCells.Should().NotBeNull();
        return draw.SlotDisplayCells!;
    }

    private async Task<CupSeed> SeedCupAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName($"Draw display {Guid.CreateVersion7():N}"),
            SampleRegulations.Standard(),
            _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        for (var index = 1; index <= 4; index++)
        {
            stage.AddSlot($"S{index}");
        }

        var entries = Enumerable.Range(0, 4).Select(_ => EntryId.New()).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        ReplaceStageDrawRules.Execute(stage, competition, new DrawRules(DrawMode.Random), _clock);
        stage.AssignEntryToSlot("S1", entries[0]);

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();
        return new CupSeed(stage.Id, entries);
    }

    private static async Task<DrawId> CreatePartialDrawAsync(HttpClient client, StageId stageId)
    {
        using var create = await client.PostAsJsonAsync(
            $"/stages/{stageId.Value}/draws",
            new CreateDrawRequest("Slot", "Partial"));
        create.EnsureSuccessStatusCode();
        var summary = await create.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);
        return new DrawId(summary!.DrawId);
    }

    private sealed record CupSeed(StageId StageId, EntryId[] Entries);
}
