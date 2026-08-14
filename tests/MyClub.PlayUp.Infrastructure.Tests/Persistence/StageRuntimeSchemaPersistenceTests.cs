// -----------------------------------------------------------------------
// <copyright file="StageRuntimeSchemaPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class StageRuntimeSchemaPersistenceTests(PostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Runtime_tables_exist_with_expected_jsonb_and_fksAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var drawColumns = await context.Database
            .SqlQueryRaw<ColumnRow>(
                """
                SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                FROM information_schema.columns
                WHERE table_name = 'draws'
                ORDER BY ordinal_position
                """)
            .ToListAsync();

        drawColumns.Select(column => column.Name).Should().Contain(["id", "stage_id", "kind", "status", "inputs", "resolution", "sort_order"]);
        drawColumns.Single(column => column.Name == "inputs").DataType.Should().Be("jsonb");
        drawColumns.Single(column => column.Name == "resolution").DataType.Should().Be("jsonb");
        drawColumns.Single(column => column.Name == "inputs").IsNullable.Should().Be("YES");
        drawColumns.Single(column => column.Name == "resolution").IsNullable.Should().Be("NO");

        var penaltyReason = await context.Database
            .SqlQueryRaw<ColumnRow>(
                """
                SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                FROM information_schema.columns
                WHERE table_name = 'penalties'
                  AND column_name = 'reason'
                """)
            .ToListAsync();

        penaltyReason.Should().ContainSingle();
        penaltyReason[0].DataType.Should().Be("text");
        penaltyReason[0].IsNullable.Should().Be("YES");

        var placementFk = await context.Database
            .SqlQueryRaw<FkRow>(
                """
                SELECT c.confdeltype AS "DeleteType"
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                WHERE t.relname = 'match_placements'
                  AND c.contype = 'f'
                  AND c.conname = 'FK_match_placements_matches_match_id'
                """)
            .ToListAsync();

        placementFk.Should().ContainSingle();
        placementFk[0].DeleteType.Should().Be('r'); // restrict

        var stagePlacementFk = await context.Database
            .SqlQueryRaw<FkRow>(
                """
                SELECT c.confdeltype AS "DeleteType"
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                WHERE t.relname = 'match_placements'
                  AND c.contype = 'f'
                  AND c.conname = 'FK_match_placements_stages_stage_id'
                """)
            .ToListAsync();

        stagePlacementFk.Should().ContainSingle();
        stagePlacementFk[0].DeleteType.Should().Be('c'); // cascade
    }

    [IntegrationFact]
    public async Task Forbidden_runtime_tables_are_absentAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var tables = await context.Database
            .SqlQueryRaw<TableRow>(
                """
                SELECT table_name AS "Name"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('qualification_results', 'progression_results', 'standings')
                """)
            .ToListAsync();

        tables.Should().BeEmpty();
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record ColumnRow(string Name, string DataType, string IsNullable);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record FkRow(char DeleteType);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record TableRow(string Name);
}
