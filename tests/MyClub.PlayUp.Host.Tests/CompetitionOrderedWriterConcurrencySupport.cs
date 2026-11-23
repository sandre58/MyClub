// -----------------------------------------------------------------------
// <copyright file="CompetitionOrderedWriterConcurrencySupport.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Holds a competition <c>pg_advisory_xact_lock</c>, queues two writers, then commits the gate in FIFO order.
/// </summary>
internal static class CompetitionOrderedWriterConcurrencySupport
{
    public static void AssertNoUnhandledPostgresContention(HttpResponseMessage response)
    {
        ((int)response.StatusCode).Should().BeLessThan(
            (int)HttpStatusCode.InternalServerError,
            "competition lock must map contention to 409, never an unhandled 500");
    }

    public static async Task AssertBodyFreeOfPostgresLockErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("40P01", "lock_not_available must not surface as an unhandled server failure");
        body.Should().NotContain("55P03");
    }

    public static Task<(object? Primary, HttpResponseMessage Secondary)> RunOrderedCompetitionWritersAsync(
        string connectionString,
        CompetitionId competitionId,
        Func<Task> primary,
        Func<Task<HttpResponseMessage>> secondary) =>
        RunOrderedCompetitionWritersCoreAsync<object?, HttpResponseMessage>(
            connectionString,
            competitionId,
            async () =>
            {
                await primary().ConfigureAwait(false);
                return null;
            },
            secondary,
            assertSecondaryHttp: true);

    public static Task<(HttpResponseMessage Primary, object? Secondary)> RunOrderedCompetitionWritersAsync(
        string connectionString,
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> primary,
        Func<Task> secondary) =>
        RunOrderedCompetitionWritersCoreAsync<HttpResponseMessage, object?>(
            connectionString,
            competitionId,
            primary,
            async () =>
            {
                await secondary().ConfigureAwait(false);
                return null;
            },
            assertPrimaryHttp: true);

    public static Task<(HttpResponseMessage Primary, HttpResponseMessage Secondary)> RunOrderedCompetitionWritersAsync(
        string connectionString,
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> primary,
        Func<Task<HttpResponseMessage>> secondary) =>
        RunOrderedCompetitionWritersCoreAsync(
            connectionString,
            competitionId,
            primary,
            secondary,
            assertPrimaryHttp: true,
            assertSecondaryHttp: true);

    private static async Task<(TPrimary Primary, TSecondary Secondary)> RunOrderedCompetitionWritersCoreAsync<TPrimary, TSecondary>(
        string connectionString,
        CompetitionId competitionId,
        Func<Task<TPrimary>> primary,
        Func<Task<TSecondary>> secondary,
        bool assertPrimaryHttp = false,
        bool assertSecondaryHttp = false)
    {
        var key = CompetitionAdvisoryLock.Key(competitionId);
        await using var gate = new NpgsqlConnection(connectionString);
        await gate.OpenAsync();
        await using var gateTransaction = await gate.BeginTransactionAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", gate, gateTransaction))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();
        }

        await using var probe = new NpgsqlConnection(connectionString);
        await probe.OpenAsync();

        var primaryTask = primary();
        await WaitUntilAsync(
            async () => await CountAdvisoryLocksAsync(probe, key, granted: false) >= 1,
            "primary writer must be queued on pg_advisory_xact_lock");
        var secondaryTask = secondary();
        await WaitUntilAsync(
            async () => await CountAdvisoryLocksAsync(probe, key, granted: false) >= 2,
            "secondary writer must be queued after primary on pg_advisory_xact_lock");

        await gateTransaction.CommitAsync();

        var primaryResponse = await primaryTask;
        var secondaryResponse = await secondaryTask;
        if (assertPrimaryHttp && primaryResponse is HttpResponseMessage primaryHttp)
        {
            await AssertBodyFreeOfPostgresLockErrorsAsync(primaryHttp);
        }

        if (assertSecondaryHttp && secondaryResponse is HttpResponseMessage secondaryHttp)
        {
            await AssertBodyFreeOfPostgresLockErrorsAsync(secondaryHttp);
        }

        return (primaryResponse, secondaryResponse);
    }

    public static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
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
        command.Parameters.AddWithValue("granted", granted);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private static (int ClassId, int ObjId) SplitAdvisoryKey(long key) =>
        ((int)(key >> 32), unchecked((int)key));
}
