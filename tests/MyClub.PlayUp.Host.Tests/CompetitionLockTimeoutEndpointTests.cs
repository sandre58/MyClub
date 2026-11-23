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
}
