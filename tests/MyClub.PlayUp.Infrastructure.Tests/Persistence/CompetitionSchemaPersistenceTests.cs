// -----------------------------------------------------------------------
// <copyright file="CompetitionSchemaPersistenceTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSchemaPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Stage_refs_unique_sort_order_constraint_is_deferrableAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var rows = await context.Database
            .SqlQueryRaw<DeferrableUniqueConstraintRow>(
                """
                SELECT c.conname AS "Name", c.condeferrable AS "IsDeferrable", c.condeferred AS "IsDeferred"
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                WHERE t.relname = 'competition_stage_refs'
                  AND c.contype = 'u'
                  AND c.conname = 'AK_competition_stage_refs_competition_id_sort_order'
                """)
            .ToListAsync();

        rows.Should().ContainSingle();
        rows[0].IsDeferrable.Should().BeTrue();
        rows[0].IsDeferred.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Delete_competition_cascades_entriesAsync()
    {
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competition.AddEntry(TeamId.New(), "Team A", _clock);
            id = competition.Id;

            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var competition = await context.Set<Competition>().SingleAsync(candidate => candidate.Id == id);
            context.Remove(competition);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

            (await context.Set<Competition>().CountAsync(candidate => candidate.Id == id)).Should().Be(0);

            var entryCount = await context.Database.SqlQueryRaw<CountRow>(
                    """
                    SELECT COUNT(*)::int AS "Value"
                    FROM competition_entries
                    WHERE competition_id = {0}
                    """,
                    id.Value)
                .SingleAsync();
            entryCount.Value.Should().Be(0);
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record DeferrableUniqueConstraintRow(string Name, bool IsDeferrable, bool IsDeferred);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    private sealed record CountRow(int Value);
}
