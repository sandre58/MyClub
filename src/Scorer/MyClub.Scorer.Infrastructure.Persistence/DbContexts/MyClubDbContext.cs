// -----------------------------------------------------------------------
// <copyright file="MyClubDbContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;

namespace MyClub.Scorer.Infrastructure.Persistence.DbContexts;

public class MyClubDbContext(DbContextOptions<MyClubDbContext> options) : DbContext(options)
{
    public DbSet<Competition> Competitions => Set<Competition>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<Matchday> Matchdays => Set<Matchday>();

    public DbSet<Round> Rounds => Set<Round>();

    public DbSet<Stage> Stages => Set<Stage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyClubDbContext).Assembly);
}
