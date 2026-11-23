// -----------------------------------------------------------------------
// <copyright file="CompetitionLockTimeoutPostgresTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionLockTimeoutPostgresTests(PostgresFixture fixture)
{
    private const string DeadlockRename = "Deadlock rename must not apply";

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 8, 0, 0, TimeSpan.Zero));

    private static string WithConnectionOption(string connectionString, string option) =>
        $"{connectionString.TrimEnd(';')};{option}";

    private static string SingleConnectionPool(string connectionString) =>
        WithConnectionOption(connectionString, "Maximum Pool Size=1");

    private static ServiceProvider CreateProvider(TimeSpan lockTimeout, string connectionString)
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(
            connectionString,
            competitionLockOptions: new PlayUpCompetitionLockOptions { LockTimeout = lockTimeout });
        return services.BuildServiceProvider(validateScopes: true);
    }

    [IntegrationFact]
    public async Task ExecuteExclusiveAsync_maps_55P03_to_competition_lock_timeoutAsync()
    {
        var competitionId = await SeedCompetitionAsync(fixture.ConnectionString);
        await using var provider = CreateProvider(TimeSpan.FromMilliseconds(500), fixture.ConnectionString);
        var key = CompetitionAdvisoryLock.Key(competitionId);

        await using var sessionConnection = new NpgsqlConnection(fixture.ConnectionString);
        await sessionConnection.OpenAsync();
        await using var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", sessionConnection);
        hold.Parameters.AddWithValue("key", key);
        _ = await hold.ExecuteScalarAsync();

        using var scope = provider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var act = () => unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            _ => Task.CompletedTask);

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(ApplicationErrorCodes.CompetitionLockTimeout);
    }

    [IntegrationFact]
    public async Task ExecuteExclusiveAsync_maps_deadlock_to_competition_lock_timeout_without_double_writeAsync()
    {
        var competitionId = await SeedCompetitionAsync(fixture.ConnectionString);
        const string originalName = "Lock timeout fixture";

        var exclusiveConnectionString = WithConnectionOption(
            fixture.ConnectionString,
            "Options=-c deadlock_timeout=100ms");
        await using var provider = CreateProvider(TimeSpan.FromSeconds(10), exclusiveConnectionString);

        var rowLockHeld = new ManualResetEventSlim(false);
        var exclusiveAttempts = 0;

        await using var blockingConnection = new NpgsqlConnection(
            WithConnectionOption(fixture.ConnectionString, "Options=-c deadlock_timeout=60s"));
        await blockingConnection.OpenAsync();
        await using var blockingTransaction = await blockingConnection.BeginTransactionAsync();

        await using (var rowLock = new NpgsqlCommand(
                "SELECT id FROM competitions WHERE id = @id FOR UPDATE",
                blockingConnection,
                blockingTransaction))
        {
            rowLock.Parameters.AddWithValue("id", competitionId.Value);
            _ = await rowLock.ExecuteScalarAsync();
        }

        var key = CompetitionAdvisoryLock.Key(competitionId);
        var exclusiveTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            try
            {
                Interlocked.Increment(ref exclusiveAttempts);
                await unitOfWork.ExecuteExclusiveAsync(
                    competitionId,
                    async cancellationToken =>
                    {
                        rowLockHeld.Set();
                        await context.Database.ExecuteSqlRawAsync(
                            "SELECT id FROM competitions WHERE id = {0} FOR UPDATE",
                            [competitionId.Value],
                            cancellationToken);
                        var competition = await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken)
                            ?? throw new InvalidOperationException("Competition missing.");
                        competition.Rename(new CompetitionName(DeadlockRename), _clock);
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    });
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        Assert.True(rowLockHeld.Wait(TimeSpan.FromSeconds(5)), "exclusive write must reach the row lock wait");

        var blockingAdvisory = Task.Run(async () =>
        {
            await using var contend = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(@key)",
                blockingConnection,
                blockingTransaction);
            contend.Parameters.AddWithValue("key", key);
            try
            {
                _ = await contend.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception) when (exception.SqlState == CompetitionLockFailure.DeadlockSqlState)
            {
                await blockingTransaction.RollbackAsync();
            }
        });

        var failure = await exclusiveTask.WaitAsync(TimeSpan.FromSeconds(15));
        await blockingAdvisory.WaitAsync(TimeSpan.FromSeconds(5));
        failure.Should().BeOfType<ApplicationFailureException>().Which.Code
            .Should().Be(ApplicationErrorCodes.CompetitionLockTimeout);
        failure.InnerException.Should().BeOfType<PostgresException>().Which.SqlState
            .Should().Be(CompetitionLockFailure.DeadlockSqlState);

        exclusiveAttempts.Should().Be(1, "exclusive writes must not be retried");

        using (var verifyScope = provider.CreateScope())
        {
            var competition = await verifyScope.ServiceProvider
                .GetRequiredService<ICompetitionRepository>()
                .GetByIdForUpdateAsync(competitionId);
            competition.Should().NotBeNull();
            competition.Name.Value.Should().Be(originalName);
        }
    }

    [IntegrationFact]
    public async Task Lock_timeout_does_not_leak_after_exclusive_write_on_pooled_connectionAsync()
    {
        var pooled = SingleConnectionPool(fixture.ConnectionString);
        var competitionId = await SeedCompetitionAsync(pooled);
        await using var provider = CreateProvider(TimeSpan.FromMilliseconds(800), pooled);

        int firstBackendPid;
        using (var scope = provider.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await unitOfWork.ExecuteExclusiveAsync(competitionId, _ => Task.CompletedTask);

            await using var connection = await OpenContextConnectionAsync(context);
            firstBackendPid = await ReadBackendPidAsync(connection);
            (await ReadLockTimeoutAsync(connection)).Should().Be("0");
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await using var connection = await OpenContextConnectionAsync(context);
            (await ReadBackendPidAsync(connection)).Should().Be(
                firstBackendPid,
                "a single-connection pool must recycle the same physical connection");
            (await ReadLockTimeoutAsync(connection)).Should().Be("0");
        }
    }

    [IntegrationFact]
    public async Task Cancelled_exclusive_wait_is_not_mapped_to_competition_lock_timeoutAsync()
    {
        var competitionId = await SeedCompetitionAsync(fixture.ConnectionString);
        await using var provider = CreateProvider(TimeSpan.FromSeconds(5), fixture.ConnectionString);
        var key = CompetitionAdvisoryLock.Key(competitionId);

        await using var sessionConnection = new NpgsqlConnection(fixture.ConnectionString);
        await sessionConnection.OpenAsync();
        await using var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", sessionConnection);
        hold.Parameters.AddWithValue("key", key);
        _ = await hold.ExecuteScalarAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        using var scope = provider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var act = () => unitOfWork.ExecuteExclusiveAsync(competitionId, _ => Task.CompletedTask, cts.Token);

        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Should().NotBeOfType<ApplicationFailureException>();
    }

    private static async Task<DbConnection> OpenContextConnectionAsync(PlayUpDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static async Task<int> ReadBackendPidAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_backend_pid()";
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task<string> ReadLockTimeoutAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT current_setting('lock_timeout')";
        return (string)(await command.ExecuteScalarAsync() ?? "0");
    }

    private async Task<CompetitionId> SeedCompetitionAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(connectionString);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var competition = Competition.Create(new CompetitionName("Lock timeout fixture"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);
        await unitOfWork.SaveChangesAsync();
        return competition.Id;
    }
}
