// -----------------------------------------------------------------------
// <copyright file="CancelGenerateDrawConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-1 — CancelDraw versus GenerateDraw under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CancelGenerateDrawConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 17, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Cancel_then_generate_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedDraftDrawReadyToGenerateAsync(factory);
            var (cancelResponse, generateResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(CancelUri(seed.StageId, seed.DrawId), content: null),
                    () => client.PostAsync(GenerateUri(seed.StageId, seed.DrawId), content: null));

            using (cancelResponse)
            using (generateResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(cancelResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);

                cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                ((int)generateResponse.StatusCode).Should().BeInRange(400, 499);
                (await CompetitionOrderedWriterConcurrencySupport.ReadCodeAsync(generateResponse))
                    .Should()
                    .Be(ApplicationErrorCodes.DrawGenerationFailure);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
            }
        }
    }

    [IntegrationFact]
    public async Task Generate_then_cancel_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedDraftDrawReadyToGenerateAsync(factory);
            var (generateResponse, cancelResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(GenerateUri(seed.StageId, seed.DrawId), content: null),
                    () => client.PostAsync(CancelUri(seed.StageId, seed.DrawId), content: null));

            using (generateResponse)
            using (cancelResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(cancelResponse);

                generateResponse.IsSuccessStatusCode.Should().BeTrue();
                cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                var draw = stage!.GetDraw(seed.DrawId);
                draw.Status.Should().Be(DrawStatus.Cancelled);
                draw.Resolution.State.Should().BeOneOf(DrawResolutionState.Resolved, DrawResolutionState.NoSolution);
            }
        }
    }

    private async Task<DrawConcurrencySeed> SeedDraftDrawReadyToGenerateAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("REST-1 cancel-generate"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entryA, entryB]));

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new DrawConcurrencySeed(competition.Id, stage.Id, draw.Id);
    }

    private static Uri CancelUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/cancel", UriKind.Relative);

    private static Uri GenerateUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/generate", UriKind.Relative);

    private sealed record DrawConcurrencySeed(CompetitionId CompetitionId, StageId StageId, DrawId DrawId);
}
