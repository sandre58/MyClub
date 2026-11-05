// -----------------------------------------------------------------------
// <copyright file="NullableMatchParticipantsMigrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public sealed class NullableMatchParticipantsMigrationTests : IAsyncLifetime
{
    private const string MigrationBeforeNullableParticipants = "20261030200141_AddResolutionProvenance";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [IntegrationFact]
    public async Task Existing_match_round_trips_without_data_loss_and_a_side_may_then_be_nullAsync()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(_container.GetConnectionString());
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        MatchId matchId;
        MatchId deferredId;
        EntryId home;
        EntryId away;

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBeforeNullableParticipants).ConfigureAwait(false);

            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Legacy"), SampleRegulations.Standard(), _clock);
            home = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
            away = competition.AddEntry(TeamId.New(), "Beta", _clock).Id;
            var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
            competition.AddStage(stage.Id, _clock);
            var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
            matchId = match.Id;
            competitions.Add(competition);
            stages.Add(stage);
            matches.Add(match);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            await context.Database.MigrateAsync().ConfigureAwait(false);

            var columns = await context.Database
                .SqlQueryRaw<ColumnRow>(
                    """
                    SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                    FROM information_schema.columns
                    WHERE table_name = 'matches'
                      AND column_name IN ('home_entry_id', 'away_entry_id')
                    ORDER BY column_name
                    """)
                .ToListAsync()
                .ConfigureAwait(false);
            columns.Should().HaveCount(2);
            columns.Should().OnlyContain(column => column.IsNullable == "YES");
        }

        using (var scope = provider.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var reloaded = await matches.GetByIdForUpdateAsync(matchId).ConfigureAwait(false);
            reloaded!.HomeEntryId.Should().Be(home);
            reloaded.AwayEntryId.Should().Be(away);

            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }

        using (var scope = provider.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var reloaded = await matches.GetByIdForUpdateAsync(matchId).ConfigureAwait(false);
            reloaded!.HomeEntryId.Should().Be(home);
            reloaded.AwayEntryId.Should().Be(away);

            var deferred = Match.Create(reloaded.CompetitionId, reloaded.StageId, home, null, _clock);
            matches.Add(deferred);
            await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
            deferredId = deferred.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var stored = await matches.GetByIdForUpdateAsync(deferredId).ConfigureAwait(false);
            stored!.HomeEntryId.Should().Be(home);
            stored.AwayEntryId.Should().BeNull();
        }
    }

    private sealed record ColumnRow(string Name, string DataType, string IsNullable);
}
