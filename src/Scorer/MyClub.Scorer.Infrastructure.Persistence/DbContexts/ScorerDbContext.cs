// -----------------------------------------------------------------------
// <copyright file="ScorerDbContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.DbContexts;

/// <summary>
/// Entity Framework Core database context for the Scorer module, managing all football
/// competition-related entities and their complex relationships through advanced EF Core patterns.
/// </summary>
public class ScorerDbContext(DbContextOptions<ScorerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the DbSet for Match entities, representing individual football matches
    /// with complete opponent information, events, and match statistics.
    /// </summary>
    public DbSet<Match> Matches => Set<Match>();

    /// <summary>
    /// Gets the DbSet for Matchday entities, representing organized collections of matches
    /// scheduled to be played during the same time period or round.
    /// </summary>
    public DbSet<Matchday> Matchdays => Set<Matchday>();

    /// <summary>
    /// Gets the DbSet for Round entities, representing tournament elimination rounds
    /// with fixtures, team progressions, and various round format configurations.
    /// </summary>
    public DbSet<Round> Rounds => Set<Round>();

    /// <summary>
    /// Gets the DbSet for Stage entities, representing distinct phases within complex
    /// multi-stage competitions like tournaments with group and knockout phases.
    /// </summary>
    public DbSet<Stage> Stages => Set<Stage>();

    /// <summary>
    /// Configures the Entity Framework model using conventions and explicit configuration classes.
    /// This method establishes the complete database schema for the football scoring domain
    /// with all relationships, constraints, and optimization strategies.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance for configuring entity mappings.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply naming and structural conventions
        // This ensures consistent database schema across all entities
        modelBuilder.ApplyConventions();

        // Apply all entity configurations found in this assembly
        // This includes specific mappings for each domain entity and their relationships
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScorerDbContext).Assembly);
    }
}
