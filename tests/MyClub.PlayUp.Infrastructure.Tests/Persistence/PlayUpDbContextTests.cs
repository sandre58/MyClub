// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContextTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class PlayUpDbContextTests
{
    [Fact]
    public void Model_maps_competition_tables_without_opening_a_connection()
    {
        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=playup_unconnected;Username=x;Password=x")
            .Options;

        using var context = new PlayUpDbContext(options);

        context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Should()
            .BeEquivalentTo("competitions", "competition_entries", "competition_stage_refs");
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");

        var stageRefs = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "competition_stage_refs");
        stageRefs.GetForeignKeys().Should().ContainSingle()
            .Which.PrincipalEntityType.GetTableName().Should().Be("competitions");
        stageRefs.GetIndexes().Should().BeEmpty();

        var entries = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "competition_entries");
        entries.GetIndexes().Should().ContainSingle(index => index.IsUnique);
        entries.GetForeignKeys().Should().ContainSingle()
            .Which.PrincipalEntityType.GetTableName().Should().Be("competitions");
    }
}
