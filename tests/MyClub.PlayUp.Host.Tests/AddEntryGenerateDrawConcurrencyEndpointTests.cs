// -----------------------------------------------------------------------
// <copyright file="AddEntryGenerateDrawConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-6 — AddEntry versus GenerateDraw under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class AddEntryGenerateDrawConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;
    private const string ExtraEntryDisplayName = "REST-6 Extra";

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 18, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task AddEntry_then_generate_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedDraftDrawReadyToGenerateAsync(factory);
            var (addResponse, generateResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsJsonAsync(
                        AddEntryUri(seed.CompetitionId),
                        new AddEntryRequest(ExtraEntryDisplayName)),
                    () => client.PostAsync(GenerateUri(seed.StageId, seed.DrawId), content: null));

            using (addResponse)
            using (generateResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);

                addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                var view = await addResponse.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
                view!.Participants.Entries.Should().Contain(entry => entry.DisplayName == ExtraEntryDisplayName);

                generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                var draw = stage!.GetDraw(seed.DrawId);
                draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
                draw.Resolution.SlotResults.Select(placement => placement.EntryId)
                    .Should()
                    .BeEquivalentTo([seed.SeedEntryA, seed.SeedEntryB]);
            }
        }
    }

    [IntegrationFact]
    public async Task Generate_then_addEntry_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedDraftDrawReadyToGenerateAsync(factory);
            var (generateResponse, addResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(GenerateUri(seed.StageId, seed.DrawId), content: null),
                    () => client.PostAsJsonAsync(
                        AddEntryUri(seed.CompetitionId),
                        new AddEntryRequest(ExtraEntryDisplayName)));

            using (generateResponse)
            using (addResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(addResponse);

                generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                var view = await addResponse.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
                view!.Participants.Entries.Should().Contain(entry => entry.DisplayName == ExtraEntryDisplayName);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                var draw = stage!.GetDraw(seed.DrawId);
                draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
                draw.Resolution.SlotResults.Select(placement => placement.EntryId)
                    .Should()
                    .BeEquivalentTo([seed.SeedEntryA, seed.SeedEntryB]);
            }
        }
    }

    private async Task<AddEntryGenerateSeed> SeedDraftDrawReadyToGenerateAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("REST-6 add-generate"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var entryA = EntryId.New();
        var entryB = EntryId.New();
        competition.AddEntry(TeamId.New(), "Seed A", entryA, _clock);
        competition.AddEntry(TeamId.New(), "Seed B", entryB, _clock);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entryA, entryB]));

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new AddEntryGenerateSeed(competition.Id, stage.Id, draw.Id, entryA, entryB);
    }

    private static Uri AddEntryUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/entries", UriKind.Relative);

    private static Uri GenerateUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/generate", UriKind.Relative);

    private sealed record AddEntryGenerateSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        DrawId DrawId,
        EntryId SeedEntryA,
        EntryId SeedEntryB);
}
