// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContextTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class PlayUpDbContextTests
{
    [Fact]
    public void Model_maps_competition_match_and_stage_tables_without_opening_a_connection()
    {
        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=playup_unconnected;Username=x;Password=x")
            .Options;

        using var context = new PlayUpDbContext(options);

        context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Should()
            .BeEquivalentTo(
                "competitions",
                "competition_entries",
                "competition_stage_refs",
                "matches",
                "stages",
                "groups",
                "group_entries",
                "rounds",
                "matchdays",
                "fixtures",
                "fixture_attachments",
                "slots",
                "stage_direct_assignments",
                "draws",
                "penalties",
                "match_placements");
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");

        var stageRefs = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "competition_stage_refs");
        stageRefs.GetForeignKeys().Should().HaveCount(2);
        stageRefs.GetForeignKeys().Should().Contain(fk => fk.PrincipalEntityType.GetTableName() == "competitions");
        stageRefs.GetForeignKeys().Should().Contain(fk =>
            fk.PrincipalEntityType.GetTableName() == "stages" && fk.DeleteBehavior == DeleteBehavior.Restrict);

        var entries = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "competition_entries");
        entries.GetIndexes().Should().ContainSingle(index => index.IsUnique);
        entries.GetForeignKeys().Should().ContainSingle()
            .Which.PrincipalEntityType.GetTableName().Should().Be("competitions");

        var matches = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "matches");
        matches.GetForeignKeys().Should().HaveCount(2);
        matches.GetForeignKeys().Should().OnlyContain(fk => fk.DeleteBehavior == DeleteBehavior.Restrict);
        matches.GetForeignKeys().Should().Contain(fk => fk.PrincipalEntityType.GetTableName() == "competitions");
        matches.GetForeignKeys().Should().Contain(fk => fk.PrincipalEntityType.GetTableName() == "stages");
        matches.FindProperty("StageId")!.IsForeignKey().Should().BeTrue();

        var stage = context.Model.FindEntityType(typeof(Stage));
        stage.Should().NotBeNull();
        stage.FindNavigation(nameof(Stage.Draws)).Should().NotBeNull();
        stage.FindNavigation(nameof(Stage.Penalties)).Should().NotBeNull();
        stage.FindNavigation(nameof(Stage.MatchPlacements)).Should().NotBeNull();
        stage.FindNavigation(nameof(Stage.DomainEvents)).Should().BeNull();

        var placements = context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "match_placements");
        placements.GetForeignKeys()
            .Should()
            .Contain(fk => fk.PrincipalEntityType.GetTableName() == "matches"
                && fk.DeleteBehavior == DeleteBehavior.Restrict);

        context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "fixtures")
            .FindProperty("round_id")
            .Should()
            .NotBeNull();
        context.Model.GetEntityTypes()
            .Single(entityType => entityType.GetTableName() == "fixtures")
            .FindProperty("matchday_id")
            .Should()
            .NotBeNull();
    }
}
