// -----------------------------------------------------------------------
// <copyright file="AddStageSlotStructureConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-4 — AddStageSlot versus ApplyDraw and RebuildStageStructure under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class AddStageSlotStructureConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const string ExtraSlotKey = "EXTRA";
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 19, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task AddStageSlot_then_applyDraw_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedPublishedSlotDrawAsync(factory);
            var (addResponse, applyResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PostAddSlotAsync(client, seed.StageId, ExtraSlotKey),
                    () => client.PostAsync(ApplyUri(seed.StageId, seed.DrawId), content: null));

            using (addResponse)
            using (applyResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);

                addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                applyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                var stage = await LoadStageAsync(factory, seed.StageId);
                stage!.FindSlot(ExtraSlotKey)!.EntryId.Should().BeNull("EXTRA stays vacant after Apply");
                stage.FindSlot("S1")!.EntryId.Should().Be(seed.EntryA);
                stage.FindSlot("S2")!.EntryId.Should().Be(seed.EntryB);
                stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
            }
        }
    }

    [IntegrationFact]
    public async Task ApplyDraw_then_addStageSlot_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedPublishedSlotDrawAsync(factory);
            var (applyResponse, addResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(ApplyUri(seed.StageId, seed.DrawId), content: null),
                    () => PostAddSlotAsync(client, seed.StageId, ExtraSlotKey));

            using (applyResponse)
            using (addResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);

                applyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                // Current rule: slot adds are not blocked once a draw is Applied.
                addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

                var stage = await LoadStageAsync(factory, seed.StageId);
                stage!.FindSlot(ExtraSlotKey).Should().NotBeNull();
                stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
            }
        }
    }

    [IntegrationFact]
    public async Task AddStageSlot_then_rebuildStructure_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedCupDraftStageAsync(factory);
            var (addResponse, rebuildResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PostAddSlotAsync(client, seed.StageId, ExtraSlotKey),
                    () => PutRebuildCupAsync(client, seed.StageId, bracketSize: 4));

            using (addResponse)
            using (rebuildResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(rebuildResponse);

                addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                rebuildResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                var rebuildBody =
                    await rebuildResponse.Content.ReadFromJsonAsync<RebuildStageStructureResponse>(HostJson.Options);
                rebuildBody.Should().NotBeNull();
                rebuildBody!.Impact.ClearedSlots.Should().BeGreaterThan(0);

                var stage = await LoadStageAsync(factory, seed.StageId);
                stage!.FindSlot(ExtraSlotKey).Should().BeNull("Rebuild clears ad-hoc slots");
            }
        }
    }

    [IntegrationFact]
    public async Task RebuildStructure_then_addStageSlot_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedCupDraftStageAsync(factory);
            var (rebuildResponse, addResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PutRebuildCupAsync(client, seed.StageId, bracketSize: 4),
                    () => PostAddSlotAsync(client, seed.StageId, ExtraSlotKey));

            using (rebuildResponse)
            using (addResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(rebuildResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);

                rebuildResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

                var stage = await LoadStageAsync(factory, seed.StageId);
                stage!.FindSlot(ExtraSlotKey).Should().NotBeNull("slot add after rebuild persists");
            }
        }
    }

    private static Task<HttpResponseMessage> PostAddSlotAsync(HttpClient client, StageId stageId, string slotKey) =>
        client.PostAsJsonAsync($"/stages/{stageId.Value}/slots", new AddStageSlotRequest(slotKey));

    private static Task<HttpResponseMessage> PutRebuildCupAsync(HttpClient client, StageId stageId, int bracketSize) =>
        client.PutAsJsonAsync(
            $"/stages/{stageId.Value}/structure",
            new RebuildStageStructureRequest(Format: "Cup", BracketSize: bracketSize));

    private static Uri ApplyUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

    private async Task<SlotDrawSeed> SeedPublishedSlotDrawAsync(PlayUpWebApplicationFactory factory)
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
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);
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
        stage.PublishDraw(draw.Id, _clock);

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new SlotDrawSeed(competition.Id, stage.Id, draw.Id, entryA, entryB);
    }

    private async Task<CupDraftSeed> SeedCupDraftStageAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("A10-rebuild-overlap", _clock);
        for (var index = 0; index < 4; index++)
        {
            AddEntry.Execute(competition, $"E{index}", _clock);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new CupDraftSeed(competition.Id, stage.Id);
    }

    private static async Task<Stage?> LoadStageAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId);
    }

    private sealed record SlotDrawSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        DrawId DrawId,
        EntryId EntryA,
        EntryId EntryB);

    private sealed record CupDraftSeed(CompetitionId CompetitionId, StageId StageId);
}
