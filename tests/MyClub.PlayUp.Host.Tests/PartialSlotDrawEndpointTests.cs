// -----------------------------------------------------------------------
// <copyright file="PartialSlotDrawEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class PartialSlotDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Partial_draw_keeps_the_manual_occupant_and_fills_only_free_slotsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupAsync(factory, slots: 4, teams: 4, occupyFirst: 1);

        using var client = factory.CreateClient();
        var drawId = await CreatePartialDrawAsync(client, seed.StageId);

        using var inputs = await client.PostAsJsonAsync(
            InputsUri(seed.StageId, drawId),
            new ConfigureDrawInputsRequest("Partial"));
        inputs.EnsureSuccessStatusCode();

        using var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null);
        generate.EnsureSuccessStatusCode();

        var afterGenerate = await ReloadAsync(factory, seed.StageId);
        afterGenerate.FindSlot("S1")!.EntryId.Should().Be(seed.Entries[0]);
        afterGenerate.Slots.Skip(1).Should().OnlyContain(slot => slot.EntryId == null);
        afterGenerate.GetDraw(drawId).Resolution.SlotResults
            .Should().NotContain(placement => placement.SlotKey == "S1");
        afterGenerate.GetDraw(drawId).Status.Should().Be(DrawStatus.Draft);

        using var redo = await client.PostAsync(RedoUri(seed.StageId, drawId), content: null);
        redo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(redo)).Should().Be(ApplicationErrorCodes.DrawRedoRequiresApplied);
        (await ReloadAsync(factory, seed.StageId)).FindSlot("S1")!.EntryId.Should().Be(seed.Entries[0]);

        using var apply = await client.PostAsync(PublishAndApplyUri(seed.StageId, drawId), content: null);
        apply.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterApply = await ReloadAsync(factory, seed.StageId);
        afterApply.FindSlot("S1")!.EntryId.Should().Be(seed.Entries[0]);
        afterApply.DirectAssignments.Should().ContainSingle(assignment => assignment.SlotKey == "S1");
        afterApply.Slots.Select(slot => slot.EntryId).Should().BeEquivalentTo(seed.Entries);
    }

    [IntegrationFact]
    public async Task Partial_inputs_return_409_when_no_slot_is_freeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupAsync(factory, slots: 2, teams: 2, occupyFirst: 2);
        using var client = factory.CreateClient();
        var drawId = await CreatePartialDrawAsync(client, seed.StageId);

        using var inputs = await client.PostAsJsonAsync(
            InputsUri(seed.StageId, drawId),
            new ConfigureDrawInputsRequest("Partial"));

        inputs.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(inputs)).Should().Be(ApplicationErrorCodes.DrawNoFreeSlot);
        var stage = await ReloadAsync(factory, seed.StageId);
        stage.Slots.Select(slot => slot.EntryId).Should().Equal(seed.Entries[0], seed.Entries[1]);
    }

    [IntegrationFact]
    public async Task Partial_inputs_return_409_when_unplaced_teams_do_not_match_free_slotsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var shortPool = await SeedCupAsync(factory, slots: 4, teams: 2, occupyFirst: 1);
        using var client = factory.CreateClient();
        var shortDraw = await CreatePartialDrawAsync(client, shortPool.StageId);
        using var notEnough = await client.PostAsJsonAsync(
            InputsUri(shortPool.StageId, shortDraw),
            new ConfigureDrawInputsRequest("Partial"));
        notEnough.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(notEnough)).Should().Be(ApplicationErrorCodes.DrawNotEnoughTeams);

        var extra = await SeedCupAsync(factory, slots: 2, teams: 3, occupyFirst: 1);
        var extraDraw = await CreatePartialDrawAsync(client, extra.StageId);
        using var tooMany = await client.PostAsJsonAsync(
            InputsUri(extra.StageId, extraDraw),
            new ConfigureDrawInputsRequest("Partial"));
        tooMany.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(tooMany)).Should().Be(ApplicationErrorCodes.DrawTooManyTeams);
        (await ReloadAsync(factory, extra.StageId)).FindSlot("S2")!.EntryId.Should().BeNull();
    }

    private async Task<CupSeed> SeedCupAsync(
        PlayUpWebApplicationFactory factory,
        int slots,
        int teams,
        int occupyFirst)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName($"Partial {slots}x{teams} {Guid.CreateVersion7():N}"),
            SampleRegulations.Standard(),
            _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        for (var index = 1; index <= slots; index++)
        {
            stage.AddSlot($"S{index}");
        }

        var entries = Enumerable.Range(0, teams).Select(_ => EntryId.New()).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        ReplaceStageDrawRules.Execute(stage, competition, new DrawRules(DrawMode.Random), _clock);
        for (var index = 0; index < occupyFirst; index++)
        {
            stage.AssignEntryToSlot($"S{index + 1}", entries[index]);
        }

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

    private static async Task<Stage> ReloadAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
        return stage!;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        return problem is null ? null : GetCode(problem);
    }

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString(),
            };

    private static string InputsUri(StageId stageId, DrawId drawId) =>
        $"/stages/{stageId.Value}/draws/{drawId.Value}/inputs";

    private static Uri GenerateUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/generate", UriKind.Relative);

    private static Uri RedoUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/redo", UriKind.Relative);

    private static Uri PublishAndApplyUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish-and-apply", UriKind.Relative);

    private sealed record CupSeed(StageId StageId, EntryId[] Entries);
}
