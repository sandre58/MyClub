// -----------------------------------------------------------------------
// <copyright file="MyClubDbContextFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Scorer.Infrastructure.Persistence.Design;

namespace MyClub.Scorer.Infrastructure.Migrations.Sqlite;

/// <summary>
/// SQLite-specific implementation of the design-time DbContext factory for Entity Framework Core
/// tooling. This factory enables SQLite migration generation, database updates, and schema
/// management operations for development and testing scenarios of the football scoring system.
/// </summary>
public class MyClubDbContextFactory : MyClubDbContextFactoryBase
{
    /// <summary>
    /// Gets the database provider name used for connection string lookup in the configuration system.
    /// This property identifies the SQLite provider for connection string resolution.
    /// </summary>
    /// <value>Returns "Sqlite" to match the connection string key in configuration files.</value>
    protected override string ProviderName => "Sqlite";

    /// <summary>
    /// Configures Entity Framework Core DbContext options specifically for SQLite, including
    /// provider configuration and migration assembly settings for design-time operations.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder to configure with SQLite-specific settings.</param>
    /// <param name="connectionString">The SQLite connection string retrieved from configuration.</param>
    protected override void ConfigureOptions(DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, string connectionString)
        => optionsBuilder.UseSqlite(connectionString, static x => x.MigrationsAssembly(Assembly.GetExecutingAssembly()));
}
