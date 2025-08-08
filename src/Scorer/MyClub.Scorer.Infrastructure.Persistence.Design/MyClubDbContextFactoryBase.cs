// -----------------------------------------------------------------------
// <copyright file="MyClubDbContextFactoryBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;

namespace MyClub.Scorer.Infrastructure.Persistence.Design;

/// <summary>
/// Abstract base class for Entity Framework Core design-time DbContext factories, providing
/// standardized configuration loading and database provider abstraction for migration tooling.
/// This factory enables EF Core tools to create DbContext instances during design-time operations.
/// </summary>
public abstract class MyClubDbContextFactoryBase : IDesignTimeDbContextFactory<ScorerDbContext>
{
    /// <summary>
    /// Gets the name of the database provider used for connection string lookup in configuration.
    /// This property must be implemented by concrete provider-specific factory classes.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Creates a new instance of ScorerDbContext for design-time operations using loaded
    /// configuration and provider-specific options. This method is called by EF Core tooling
    /// to obtain DbContext instances for migration and schema operations.
    /// </summary>
    /// <param name="args">Command-line arguments passed from EF Core tools (typically unused).</param>
    /// <returns>A fully configured ScorerDbContext instance ready for design-time operations.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection string for the specified provider is not found in configuration.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the configuration directory cannot be located using any discovery strategy.
    /// </exception>
    public ScorerDbContext CreateDbContext(string[] args)
    {
        var configuration = LoadConfiguration();

        var connectionString = configuration.GetConnectionString(ProviderName) ?? throw new InvalidOperationException($"Missing {ProviderName} connection string.");

        var optionsBuilder = new DbContextOptionsBuilder<ScorerDbContext>();
        ConfigureOptions(optionsBuilder, connectionString);

        return new(optionsBuilder.Options);
    }

    /// <summary>
    /// Configures the DbContextOptionsBuilder with provider-specific settings and connection string.
    /// This abstract method must be implemented by concrete factory classes to provide database
    /// provider-specific configuration including migration assembly settings.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure with provider-specific settings.</param>
    /// <param name="connectionString">The connection string retrieved from configuration for this provider.</param>
    protected abstract void ConfigureOptions(DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, string connectionString);

    /// <summary>
    /// Loads configuration from JSON files and environment variables using the .NET configuration
    /// system with environment-aware overrides. This method provides comprehensive configuration
    /// loading with intelligent file discovery and hierarchical configuration merging.
    /// </summary>
    /// <returns>An IConfiguration instance containing loaded settings from all configuration sources.</returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the configuration directory cannot be found using any discovery strategy.
    /// </exception>
    private static IConfiguration LoadConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        // Find the config directory - it should be in the Design project
        var configPath = FindConfigDirectory();

        return !Directory.Exists(configPath)
            ? throw new DirectoryNotFoundException($"Configuration directory not found at: {configPath}")
            : (IConfiguration)new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Implements intelligent configuration directory discovery using multiple fallback strategies
    /// to locate configuration files across different project structures and execution contexts.
    /// This method ensures configuration files can be found in various development and deployment scenarios.
    /// </summary>
    /// <returns>The full path to the configuration directory containing appsettings files.</returns>
    private static string FindConfigDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        // Strategy 1: Look for config in current directory
        var configPath = Path.Combine(currentDirectory, "config");
        if (Directory.Exists(configPath))
            return configPath;

        // Strategy 2: Look for config in Design project relative to Migrations project
        // Navigate from Migrations.SqlServer or Migrations.Sqlite to Design project
        var designProjectPath = Path.Combine(currentDirectory, "..", "MyClub.Scorer.Infrastructure.Persistence.Design", "config");
        return Directory.Exists(designProjectPath) ? Path.GetFullPath(designProjectPath) :
            Path.Combine(currentDirectory, "config");
    }
}
