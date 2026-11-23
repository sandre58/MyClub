// -----------------------------------------------------------------------
// <copyright file="CompetitionLockTimeoutPostgresTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 8, 0, 0, TimeSpan.Zero));

    private ServiceProvider CreateProvider(TimeSpan lockTimeout)
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(
            fixture.ConnectionString,
            competitionLockOptions: new PlayUpCompetitionLockOptions { LockTimeout = lockTimeout });
        return services.BuildServiceProvider(validateScopes: true);
    }

    [IntegrationFact]
    public async Task ExecuteExclusiveAsync_maps_55P03_to_competition_lock_timeoutAsync()
    {
        var competitionId = await SeedCompetitionAsync();
        await using var provider = CreateProvider(TimeSpan.FromMilliseconds(500));
        var key = CompetitionAdvisoryLock.Key(competitionId);

        await using var sessionConnection = new NpgsqlConnection(fixture.ConnectionString);
        await sessionConnection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", sessionConnection))
        {
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
    }

    [IntegrationFact]
    public async Task ExecuteExclusiveAsync_maps_deadlock_to_competition_lock_timeout_without_double_writeAsync()
    {
        var competitionId = await SeedCompetitionAsync();
        await using var provider = CreateProvider(TimeSpan.FromSeconds(5));

        await using var blockingConnection = new NpgsqlConnection(fixture.ConnectionString);
        await blockingConnection.OpenAsync();
        await using var blockingTransaction = await blockingConnection.BeginTransactionAsync();

        await using var rowLock = new NpgsqlCommand(
            "SELECT id FROM competitions WHERE id = @id FOR UPDATE",
            blockingConnection,
            blockingTransaction);
        rowLock.Parameters.AddWithValue("id", competitionId.Value);
        _ = await rowLock.ExecuteScalarAsync();

        var exclusiveTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            try
            {
                await unitOfWork.ExecuteExclusiveAsync(
                    competitionId,
                    async cancellationToken =>
                    {
                        await context.Database.ExecuteSqlRawAsync(
                            "SELECT id FROM competitions WHERE id = {0} FOR UPDATE",
                            [competitionId.Value],
                            cancellationToken);
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    });
                return (Exception?)null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        await Task.Delay(TimeSpan.FromMilliseconds(400));

        var key = CompetitionAdvisoryLock.Key(competitionId);
        try
        {
            await using var contend = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(@key)",
                blockingConnection,
                blockingTransaction);
            contend.Parameters.AddWithValue("key", key);
            _ = await contend.ExecuteNonQueryAsync();
        }
        catch (PostgresException)
        {
            // Deadlock victim may abort the statement here; the exclusive path is what we assert on.
        }

        var failure = await exclusiveTask;
        failure.Should().BeOfType<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CompetitionLockTimeout);

        using (var verifyScope = provider.CreateScope())
        {
            var competition = await verifyScope.ServiceProvider
                .GetRequiredService<ICompetitionRepository>()
                .GetByIdForUpdateAsync(competitionId);
            competition.Should().NotBeNull();
            competition!.Name.Value.Should().Be("Lock timeout fixture");
        }
    }

    [IntegrationFact]
    public async Task Lock_timeout_does_not_leak_after_exclusive_write_on_pooled_connectionAsync()
    {
        var competitionId = await SeedCompetitionAsync();
        var oneConnectionPool = fixture.ConnectionString + ";Maximum Pool Size=1";
        await using var provider = CreateProvider(TimeSpan.FromMilliseconds(800));
        await using var probe = new NpgsqlConnection(oneConnectionPool);
        await probe.OpenAsync();

        for (var iteration = 0; iteration < 2; iteration++)
        {
            using var scope = provider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.ExecuteExclusiveAsync(competitionId, _ => Task.CompletedTask);

            await using var setting = new NpgsqlCommand("SELECT current_setting('lock_timeout')", probe);
            var raw = (string)(await setting.ExecuteScalarAsync() ?? "0");
            raw.Should().NotBe("800ms", $"iteration {iteration} must not keep SET LOCAL lock_timeout");
        }
    }

    [IntegrationFact]
    public async Task Cancelled_exclusive_wait_is_not_mapped_to_competition_lock_timeoutAsync()
    {
        var competitionId = await SeedCompetitionAsync();
        await using var provider = CreateProvider(TimeSpan.FromSeconds(5));
        var key = CompetitionAdvisoryLock.Key(competitionId);

        await using var sessionConnection = new NpgsqlConnection(fixture.ConnectionString);
        await sessionConnection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", sessionConnection))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            using var scope = provider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var act = () => unitOfWork.ExecuteExclusiveAsync(
                competitionId,
                _ => Task.CompletedTask,
                cts.Token);

            var exception = await act.Should().ThrowAsync<Exception>();
            exception.Which.Should().NotBeOfType<ApplicationFailureException>();
        }
    }

    private async Task<CompetitionId> SeedCompetitionAsync()
    {
        using var scope = fixture.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var competition = Competition.Create(new CompetitionName("Lock timeout fixture"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);
        await unitOfWork.SaveChangesAsync();
        return competition.Id;
    }
}
