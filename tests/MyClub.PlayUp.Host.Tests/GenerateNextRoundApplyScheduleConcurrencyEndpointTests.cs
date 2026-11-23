// -----------------------------------------------------------------------
// <copyright file="GenerateNextRoundApplyScheduleConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-3 — GenerateNextRound / ApplySchedule versus DematerializePhase under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class GenerateNextRoundApplyScheduleConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 19, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Dematerialize_then_generateNextRound_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissWithRoundOneAsync(factory, client);
            var (dematerializeResponse, generateResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PostDematerializeAsync(client, seed.StageId),
                    () => client.PostAsync(GenerateNextRoundUri(seed.StageId), content: null));

            using (dematerializeResponse)
            using (generateResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(dematerializeResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);

                dematerializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                var dematerializeBody =
                    await dematerializeResponse.Content.ReadFromJsonAsync<DematerializePhaseResponse>(HostJson.Options);
                dematerializeBody.Should().NotBeNull();
                dematerializeBody!.RemovedRoundNumber.Should().Be(1);

                // Current rule: after dematerializing the last Swiss round, a new GenerateNextRound recreates round 1.
                generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                (await CountStageMatchesAsync(factory, seed.StageId)).Should().Be(2);
            }
        }
    }

    [IntegrationFact]
    public async Task GenerateNextRound_then_dematerialize_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissWithRoundOneAsync(factory, client);
            var matchCountBefore = await CountStageMatchesAsync(factory, seed.StageId);

            var (generateResponse, dematerializeResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(GenerateNextRoundUri(seed.StageId), content: null),
                    () => PostDematerializeAsync(client, seed.StageId));

            using (generateResponse)
            using (dematerializeResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(generateResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(dematerializeResponse);

                generateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                (await CompetitionOrderedWriterConcurrencySupport.ReadCodeAsync(generateResponse))
                    .Should()
                    .Be(ApplicationErrorCodes.SwissRoundGenerationFailure);

                dematerializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                (await CountStageMatchesAsync(factory, seed.StageId)).Should().BeLessThan(matchCountBefore);
                (await CountStageMatchesAsync(factory, seed.StageId)).Should().Be(0);
            }
        }
    }

    [IntegrationFact]
    public async Task Dematerialize_then_applySchedule_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissWithRoundOneAsync(factory, client);

            var (dematerializeResponse, applyResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PostDematerializeAsync(client, seed.StageId),
                    () => PostApplyScheduleAsync(client, seed.StageId, seed.ApplyRequest));

            using (dematerializeResponse)
            using (applyResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(dematerializeResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);

                dematerializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                applyResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
                (await CompetitionOrderedWriterConcurrencySupport.ReadCodeAsync(applyResponse))
                    .Should()
                    .Be(StageErrorCodes.MatchPlacementInvalid);
            }
        }
    }

    [IntegrationFact]
    public async Task ApplySchedule_then_dematerialize_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissWithRoundOneAsync(factory, client);
            var matchCountBefore = await CountStageMatchesAsync(factory, seed.StageId);

            var (applyResponse, dematerializeResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PostApplyScheduleAsync(client, seed.StageId, seed.ApplyRequest),
                    () => PostDematerializeAsync(client, seed.StageId));

            using (applyResponse)
            using (dematerializeResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(dematerializeResponse);

                applyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                dematerializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                (await CountStageMatchesAsync(factory, seed.StageId)).Should().BeLessThan(matchCountBefore);
                (await CountStageMatchesAsync(factory, seed.StageId)).Should().Be(0);
            }
        }
    }

    private async Task<SwissScheduleStressSeed> SeedSwissWithRoundOneAsync(
        PlayUpWebApplicationFactory factory,
        HttpClient client)
    {
        var stageId = await SeedRunningSwissAsync(factory);
        using (var response = await client.PostAsync(GenerateNextRoundUri(stageId), content: null))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var scope = factory.Services.CreateScope();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
        stageMatches.Should().HaveCount(2);
        var targetMatch = stageMatches[0];
        var competitionId = targetMatch.CompetitionId;
        var start = _clock.UtcNow;
        var applyRequest = new ApplyScheduleRequest(
            [new ScheduleAssignmentDto(targetMatch.Id.Value, start, Guid.NewGuid())],
            [targetMatch.Id.Value]);
        return new SwissScheduleStressSeed(competitionId, stageId, applyRequest);
    }

    private async Task<StageId> SeedRunningSwissAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("REST-3-swiss-overlap", _clock);
        for (var index = 0; index < 4; index++)
        {
            AddEntry.Execute(competition, $"T{index}", _clock);
        }

        var stage = Stage.Create(competition.Id, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(3));
        competition.AddStage(stage.Id, _clock);
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }

    private static async Task<int> CountStageMatchesAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stageId);
        return list.Count;
    }

    private static Task<HttpResponseMessage> PostDematerializeAsync(HttpClient client, StageId stageId) =>
        client.PostAsync(
            new Uri($"/stages/{stageId.Value}/matches/dematerialize", UriKind.Relative),
            content: null);

    private static Task<HttpResponseMessage> PostApplyScheduleAsync(
        HttpClient client,
        StageId stageId,
        ApplyScheduleRequest request) =>
        client.PostAsJsonAsync(new Uri($"/stages/{stageId.Value}/schedule/apply", UriKind.Relative), request);

    private static Uri GenerateNextRoundUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/swiss/generate-next-round", UriKind.Relative);

    private sealed record SwissScheduleStressSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        ApplyScheduleRequest ApplyRequest);
}
