// -----------------------------------------------------------------------
// <copyright file="MatchSchemaPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class MatchSchemaPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Matches_table_exists_with_expected_columnsAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var columns = await context.Database
            .SqlQueryRaw<ColumnRow>(
                """
                SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                FROM information_schema.columns
                WHERE table_name = 'matches'
                ORDER BY ordinal_position
                """)
            .ToListAsync();

        columns.Select(column => column.Name).Should().Equal(
            "id",
            "competition_id",
            "stage_id",
            "home_entry_id",
            "away_entry_id",
            "status",
            "result");
        columns.Single(column => column.Name == "result").DataType.Should().Be("jsonb");
        columns.Single(column => column.Name == "result").IsNullable.Should().Be("YES");
        columns.Where(column => column.Name != "result").Should().OnlyContain(column => column.IsNullable == "NO");
    }

    [IntegrationFact]
    public async Task Delete_competition_with_match_is_restrictedAsync()
    {
        CompetitionId competitionId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitionId = competition.Id;
            competitions.Add(competition);
            matches.Add(Match.Create(competitionId, StageId.New(), EntryId.New(), EntryId.New(), _clock));
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var competition = await context.Set<Competition>().SingleAsync(candidate => candidate.Id == competitionId);
            context.Remove(competition);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            (await context.Set<Competition>().CountAsync(candidate => candidate.Id == competitionId)).Should().Be(1);
            (await context.Set<Match>().CountAsync(candidate => candidate.CompetitionId == competitionId)).Should().Be(1);
        }
    }

    [IntegrationFact]
    public async Task Stage_id_has_no_foreign_key_constraintAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var foreignKeys = await context.Database
            .SqlQueryRaw<ForeignKeyRow>(
                """
                SELECT c.conname AS "Name"
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                WHERE t.relname = 'matches'
                  AND c.contype = 'f'
                """)
            .ToListAsync();

        foreignKeys.Should().ContainSingle()
            .Which.Name.Should().Be("FK_matches_competitions_competition_id");
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record ColumnRow(string Name, string DataType, string IsNullable);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record ForeignKeyRow(string Name);
}
