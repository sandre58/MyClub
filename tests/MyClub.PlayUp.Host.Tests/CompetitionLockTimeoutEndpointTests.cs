// -----------------------------------------------------------------------
// <copyright file="CompetitionLockTimeoutEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionLockTimeoutEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Recorded_goal_post_returns_409_when_session_lock_outlasts_lock_timeoutAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(
            fixture.ConnectionString,
            competitionLockTimeout: "500");
        var seed = await SeedLiveMatchWithScorerAsync(factory);
        var key = CompetitionAdvisoryLock.Key(seed.CompetitionId);
        using var client = factory.CreateClient();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();

            using var response = await client.PostAsJsonAsync(
                RecordedGoalsUri(seed.MatchId),
                new RecordGoalRequest(seed.ScorerMemberId.Value, Side.Home, null));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.CompetitionLockTimeout);
            response.Headers.RetryAfter.Should().BeNull();
            (await CountRecordedGoalsAsync(factory, seed.MatchId)).Should().Be(0);
        }
    }

    [IntegrationFact]
    public async Task Http_exclusive_write_returns_409_when_session_lock_outlasts_lock_timeoutAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(
            fixture.ConnectionString,
            competitionLockTimeout: "500");
        var seed = await SeedGroupsAsync(factory);
        var key = CompetitionAdvisoryLock.Key(seed.CompetitionId);
        using var client = factory.CreateClient();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();

            var response = await client.PutAsJsonAsync(
                $"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/membership",
                new SetGroupEntriesRequest(
                    seed.EntryIds.Select(id => id.Value).ToArray(),
                    []));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.CompetitionLockTimeout);
        }
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

    private static Uri RecordedGoalsUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/recorded-goals", UriKind.Relative);

    private async Task<LiveMatchSeed> SeedLiveMatchWithScorerAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("Lock-timeout-record-goal", _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var scorer = competition.AddDeclaredMember(home.Id, "Scorer", DeclaredMemberRole.Player, _clock).Id;

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring([home.Id, away.Id], _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(scorer, Side.Home, CompositionStatus.Starter, _clock);
        match.Start(_clock);

        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
        scope.ServiceProvider.GetRequiredService<IMatchRepository>().Add(match);
        await unitOfWork.SaveChangesAsync();
        return new LiveMatchSeed(competition.Id, match.Id, scorer);
    }

    private async Task<Seed> SeedGroupsAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("Lock-timeout-host", _clock);
        var entries = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"E{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(entries, _clock);

        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
        await unitOfWork.SaveChangesAsync();
        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, entries);
    }

    private sealed record Seed(
        CompetitionId CompetitionId,
        StageId StageId,
        GroupId GroupId,
        IReadOnlyList<EntryId> EntryIds);

    private sealed record LiveMatchSeed(
        CompetitionId CompetitionId,
        MatchId MatchId,
        MemberId ScorerMemberId);

    private static async Task<int> CountRecordedGoalsAsync(PlayUpWebApplicationFactory factory, MatchId matchId)
    {
        using var scope = factory.Services.CreateScope();
        var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .GetByIdReadOnlyAsync(matchId);
        return match?.RecordedGoals.Count ?? 0;
    }
}
