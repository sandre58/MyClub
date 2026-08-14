// -----------------------------------------------------------------------
// <copyright file="StageSchemaPersistenceTests.cs" company="Stéphane ANDRE">
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
public sealed class StageSchemaPersistenceTests(PostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Stages_stage_regulation_column_is_jsonb_not_nullAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var columns = await context.Database
            .SqlQueryRaw<ColumnRow>(
                """
                SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                FROM information_schema.columns
                WHERE table_name = 'stages'
                  AND column_name = 'stage_regulation'
                """)
            .ToListAsync();

        columns.Should().ContainSingle();
        columns[0].DataType.Should().Be("jsonb");
        columns[0].IsNullable.Should().Be("NO");
    }

    [IntegrationFact]
    public async Task Rounds_tie_format_column_is_jsonb_nullableAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var columns = await context.Database
            .SqlQueryRaw<ColumnRow>(
                """
                SELECT column_name AS "Name", data_type AS "DataType", is_nullable AS "IsNullable"
                FROM information_schema.columns
                WHERE table_name = 'rounds'
                  AND column_name = 'tie_format'
                """)
            .ToListAsync();

        columns.Should().ContainSingle();
        columns[0].DataType.Should().Be("jsonb");
        columns[0].IsNullable.Should().Be("YES");
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test")]
    private sealed record ColumnRow(string Name, string DataType, string IsNullable);
}
