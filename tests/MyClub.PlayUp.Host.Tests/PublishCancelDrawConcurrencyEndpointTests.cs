// -----------------------------------------------------------------------
// <copyright file="PublishCancelDrawConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
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
/// A10-REST-1 — PublishDraw versus CancelDraw under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class PublishCancelDrawConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 17, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Cancel_then_publish_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedResolvedDraftDrawAsync(factory);
            var (cancelResponse, publishResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(CancelUri(seed.StageId, seed.DrawId), content: null),
                    () => client.PostAsync(PublishUri(seed.StageId, seed.DrawId), content: null));

            using (cancelResponse)
            using (publishResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(cancelResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(publishResponse);

                cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                publishResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
                (await CompetitionOrderedWriterConcurrencySupport.ReadCodeAsync(publishResponse))
                    .Should()
                    .Be(StageErrorCodes.DrawInvalidTransition);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
            }
        }
    }

    [IntegrationFact]
    public async Task Publish_then_cancel_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedResolvedDraftDrawAsync(factory);
            var (publishResponse, cancelResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(PublishUri(seed.StageId, seed.DrawId), content: null),
                    () => client.PostAsync(CancelUri(seed.StageId, seed.DrawId), content: null));

            using (publishResponse)
            using (cancelResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(publishResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(cancelResponse);

                publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                // Current business rule: Cancel remains allowed on Published; second writer ends Cancelled.
                cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                using var scope = factory.Services.CreateScope();
                var stage = await scope.ServiceProvider
                    .GetRequiredService<IStageRepository>()
                    .GetByIdForUpdateAsync(seed.StageId);
                stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
            }
        }
    }

    private async Task<DrawConcurrencySeed> SeedResolvedDraftDrawAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("REST-1 publish-cancel"), SampleRegulations.Standard(), _clock);
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

        return new DrawConcurrencySeed(competition.Id, stage.Id, draw.Id);
    }

    private static Uri PublishUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish", UriKind.Relative);

    private static Uri CancelUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/cancel", UriKind.Relative);

    private sealed record DrawConcurrencySeed(CompetitionId CompetitionId, StageId StageId, DrawId DrawId);
}
