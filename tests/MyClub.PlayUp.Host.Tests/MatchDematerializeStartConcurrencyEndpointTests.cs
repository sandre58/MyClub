// -----------------------------------------------------------------------
// <copyright file="MatchDematerializeStartConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-R7 — Swiss running: dematerialize last round versus StartMatch under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class MatchDematerializeStartConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Start_then_dematerialize_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissRunningWithLastRoundAsync(factory, client);
            var (startResponse, dematerializeResponse) = await RunOrderedCompetitionWritersAsync(
                seed.CompetitionId,
                () => client.PostAsync(StartUri(seed.MatchId), content: null),
                () => PostDematerializeAsync(client, seed.StageId));

            using (startResponse)
            using (dematerializeResponse)
            {
                AssertNoUnhandledPostgresContention(startResponse);
                AssertNoUnhandledPostgresContention(dematerializeResponse);

                startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                (await ReadCodeAsync(dematerializeResponse))
                    .Should().Be(ApplicationErrorCodes.DematerializationBlocked);

                var match = await ReadMatchAsync(factory, seed.MatchId);
                match.Should().NotBeNull();
                match.Status.Should().Be(MatchStatus.Live);
            }
        }
    }

    [IntegrationFact]
    public async Task Dematerialize_then_start_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedSwissRunningWithLastRoundAsync(factory, client);
            var matchCountBefore = await CountStageMatchesAsync(factory, seed.StageId);

            var (dematerializeResponse, startResponse) = await RunOrderedCompetitionWritersAsync(
                seed.CompetitionId,
                () => PostDematerializeAsync(client, seed.StageId),
                () => client.PostAsync(StartUri(seed.MatchId), content: null));

            using (dematerializeResponse)
            using (startResponse)
            {
                AssertNoUnhandledPostgresContention(dematerializeResponse);
                AssertNoUnhandledPostgresContention(startResponse);

                dematerializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                startResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
                (await ReadCodeAsync(startResponse)).Should().Be(ApplicationErrorCodes.MatchNotFound);

                (await CountStageMatchesAsync(factory, seed.StageId)).Should().BeLessThan(matchCountBefore);
                (await ReadMatchAsync(factory, seed.MatchId)).Should().BeNull();
            }
        }
    }

    private static void AssertNoUnhandledPostgresContention(HttpResponseMessage response) =>
        ((int)response.StatusCode).Should().BeLessThan(
            (int)HttpStatusCode.InternalServerError,
            "competition lock must map contention to 409, never an unhandled 500");

    private static async Task AssertBodyFreeOfPostgresLockErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("40P01", "lock_not_available must not surface as an unhandled server failure");
        body.Should().NotContain("55P03");
    }

    private async Task<SwissStressSeed> SeedSwissRunningWithLastRoundAsync(
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
        var scheduled = stageMatches.First(match => match.Status == MatchStatus.Scheduled);
        return new SwissStressSeed(scheduled.CompetitionId, stageId, scheduled.Id);
    }

    private async Task<StageId> SeedRunningSwissAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("R7-swiss-dematerialize-start", _clock);
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

    /// <summary>
    /// Holds the competition <c>pg_advisory_xact_lock</c>, queues <paramref name="primary"/> then
    /// <paramref name="secondary"/>, verifies both are waiting in <c>pg_locks</c>, then commits to
    /// release in FIFO order (primary wins the race).
    /// </summary>
    private async Task<(HttpResponseMessage Primary, HttpResponseMessage Secondary)> RunOrderedCompetitionWritersAsync(
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> primary,
        Func<Task<HttpResponseMessage>> secondary)
    {
        var key = CompetitionAdvisoryLock.Key(competitionId);
        await using var gate = new NpgsqlConnection(fixture.ConnectionString);
        await gate.OpenAsync();
        await using var gateTransaction = await gate.BeginTransactionAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", gate, gateTransaction))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();
        }

        await using var probe = new NpgsqlConnection(fixture.ConnectionString);
        await probe.OpenAsync();

        var primaryTask = primary();
        await WaitUntilAsync(
            async () => await CountAdvisoryLocksAsync(probe, key, granted: false) >= 1,
            "primary HTTP writer must be queued on pg_advisory_xact_lock");
        var secondaryTask = secondary();
        await WaitUntilAsync(
            async () => await CountAdvisoryLocksAsync(probe, key, granted: false) >= 2,
            "secondary HTTP writer must be queued after primary on pg_advisory_xact_lock");

        await gateTransaction.CommitAsync();

        var primaryResponse = await primaryTask;
        var secondaryResponse = await secondaryTask;
        await AssertBodyFreeOfPostgresLockErrorsAsync(primaryResponse);
        await AssertBodyFreeOfPostgresLockErrorsAsync(secondaryResponse);
        return (primaryResponse, secondaryResponse);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, string because, int attempts = 200)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException(because);
    }

    private static async Task<int> CountAdvisoryLocksAsync(
        NpgsqlConnection connection,
        long key,
        bool granted)
    {
        var (classId, objId) = SplitAdvisoryKey(key);
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)::int
            FROM pg_locks
            WHERE locktype = 'advisory'
              AND classid = @classId
              AND objid = @objId
              AND granted = @granted
            """,
            connection);
        command.Parameters.AddWithValue("classId", classId);
        command.Parameters.AddWithValue("objId", objId);
        command.Parameters.AddWithValue(nameof(granted), granted);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private static (int ClassId, int ObjId) SplitAdvisoryKey(long key) =>
        ((int)(key >> 32), unchecked((int)key));

    private static async Task<Match?> ReadMatchAsync(PlayUpWebApplicationFactory factory, MatchId matchId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .GetByIdReadOnlyAsync(matchId);
    }

    private static async Task<int> CountStageMatchesAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stageId);
        return list.Count;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        if (problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => raw.ToString()
        };
    }

    private static Task<HttpResponseMessage> PostDematerializeAsync(HttpClient client, StageId stageId) =>
        client.PostAsync(
            new Uri($"/stages/{stageId.Value}/matches/dematerialize", UriKind.Relative),
            content: null);

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private static Uri GenerateNextRoundUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/swiss/generate-next-round", UriKind.Relative);

    private sealed record SwissStressSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        MatchId MatchId);
}
