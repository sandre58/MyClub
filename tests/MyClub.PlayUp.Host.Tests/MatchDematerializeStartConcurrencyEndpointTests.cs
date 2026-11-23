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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-R7 lot 3 — DematerializePhase versus StartMatch under competition lock (R7 race).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class MatchDematerializeStartConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Parallel_dematerialize_then_start_never_500_or_40P01_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedMaterializedChampionshipAsync(factory);
            var responses = await OverlapOnCompetitionLockAsync(
                seed.CompetitionId,
                () => PostDematerializeAsync(client, seed.StageId),
                () => client.PostAsync(StartUri(seed.MatchId), content: null));
            await AssertRaceOutcomesAsync(responses);
        }
    }

    [IntegrationFact]
    public async Task Parallel_start_then_dematerialize_never_500_or_40P01_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedMaterializedChampionshipAsync(factory);
            var responses = await OverlapOnCompetitionLockAsync(
                seed.CompetitionId,
                () => client.PostAsync(StartUri(seed.MatchId), content: null),
                () => PostDematerializeAsync(client, seed.StageId));
            await AssertRaceOutcomesAsync(responses);
        }
    }

    private static async Task AssertRaceOutcomesAsync(HttpResponseMessage[] responses)
    {
        foreach (var response in responses)
        {
            using (response)
            {
                ((int)response.StatusCode).Should().BeLessThan(
                    (int)HttpStatusCode.InternalServerError,
                    "competition lock must map contention to 409, never an unhandled 500");
                var body = await response.Content.ReadAsStringAsync();
                body.Should().NotContain(
                    "40P01",
                    "lock_not_available must not surface as an unhandled server failure");
                body.Should().NotContain("55P03");
            }
        }
    }

    private async Task<ChampionshipStressSeed> SeedMaterializedChampionshipAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("R7-dematerialize-start", _clock);
        for (var index = 0; index < 4; index++)
        {
            AddEntry.Execute(competition, $"T{index}", _clock);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        var entries = competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        stage.Prepare(_clock);
        var created = MaterializeMatches.Execute(
            competition,
            stage,
            existingMatches: [],
            _clock,
            competitionStages: [stage]).CreatedMatches;
        created.Should().NotBeEmpty();

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        foreach (var match in created)
        {
            matches.Add(match);
        }

        await unitOfWork.SaveChangesAsync();
        var scheduled = created.First(match => match.Status == MatchStatus.Scheduled);
        return new ChampionshipStressSeed(competition.Id, stage.Id, scheduled.Id);
    }

    private async Task<HttpResponseMessage[]> OverlapOnCompetitionLockAsync(
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> left,
        Func<Task<HttpResponseMessage>> right)
    {
        var key = CompetitionAdvisoryLock.Key(competitionId);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();
        }

        var leftTask = left();
        var rightTask = right();
        var overlapped = false;
        try
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                await using var count = new NpgsqlCommand(
                    "SELECT COUNT(*)::int FROM pg_locks WHERE locktype = 'advisory' AND NOT granted",
                    connection);
                var waiting = (int)(await count.ExecuteScalarAsync() ?? 0);
                if (waiting >= 2)
                {
                    overlapped = true;
                    break;
                }

                await Task.Delay(50);
            }
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
            release.Parameters.AddWithValue("key", key);
            _ = await release.ExecuteScalarAsync();
        }

        var responses = await Task.WhenAll(leftTask, rightTask);
        overlapped.Should().BeTrue("both writers must be waiting on the competition advisory lock");
        return responses;
    }

    private static Task<HttpResponseMessage> PostDematerializeAsync(HttpClient client, StageId stageId) =>
        client.PostAsync(
            new Uri($"/stages/{stageId.Value}/matches/dematerialize", UriKind.Relative),
            content: null);

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private sealed record ChampionshipStressSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        MatchId MatchId);
}
