// -----------------------------------------------------------------------
// <copyright file="ModularArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Referential.Domain.TeamAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate modular architecture patterns and inter-module dependencies.
/// Ensures proper module isolation and dependency management across the MyClub suite.
/// </summary>
public class ModularArchitectureTests(ITestOutputHelper output)
{
    private static readonly Assembly ScorerDomainAssembly = typeof(Competition).Assembly;
    private static readonly Assembly ScorerApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;
    private static readonly Assembly ReferentialDomainAssembly = typeof(Team).Assembly;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Modules_Should_Not_Have_Circular_Dependencies()
    {
        // Check Scorer -> Referential (should not exist)
        var scorerToReferential = Types.InAssembly(ScorerDomainAssembly)
            .That()
            .HaveDependencyOn("MyClub.Referential")
            .GetTypes();

        var scorerAppToReferential = Types.InAssembly(ScorerApplicationAssembly)
            .That()
            .HaveDependencyOn("MyClub.Referential")
            .GetTypes();

        // Check Referential -> Scorer (should not exist)
        var referentialToScorer = Types.InAssembly(ReferentialDomainAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer")
            .GetTypes();

        // Assert
        scorerToReferential.Should().BeEmpty("Scorer module should not depend on Referential module");
        scorerAppToReferential.Should().BeEmpty("Scorer Application should not depend on Referential module");
        referentialToScorer.Should().BeEmpty("Referential module should not depend on Scorer module");

        _output.WriteLine("✓ No circular dependencies found between Scorer and Referential modules");
    }

    [Fact]
    public void Referential_Module_Should_Be_Independent()
    {
        // Arrange & Act
        var result = Types.InAssembly(ReferentialDomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MyClub.Scorer",
                "MyClub.TeamUp", // Future module
                "MyClub.Training", // Future module
                "MyClub.Licensing") // Future module
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Referential module should be independent of other business modules. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Scorer_Module_Should_Not_Depend_On_Future_Modules()
    {
        // Arrange & Act
        var domainResult = Types.InAssembly(ScorerDomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MyClub.TeamUp",
                "MyClub.Training",
                "MyClub.Licensing")
            .GetResult();

        var applicationResult = Types.InAssembly(ScorerApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MyClub.TeamUp",
                "MyClub.Training",
                "MyClub.Licensing")
            .GetResult();

        // Assert
        domainResult.IsSuccessful.Should().BeTrue($"Scorer Domain should not depend on future modules. Failures: {string.Join(", ", domainResult.FailingTypeNames ?? [])}");
        applicationResult.IsSuccessful.Should().BeTrue($"Scorer Application should not depend on future modules. Failures: {string.Join(", ", applicationResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Migration_Assemblies_Should_Be_Properly_Separated()
    {
        // Force loading of shared infrastructure assembly
        var forceLoad = typeof(MyClub.Scorer.Infrastructure.Migrations.SqlServer.MyClubDbContextFactory);

        // Arrange & Act
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var migrationAssemblies = assemblies
            .Where(a => a.GetName().Name?.Contains("Migrations", StringComparison.InvariantCulture) == true)
            .ToList();

        var sqliteMigrations = migrationAssemblies
            .Where(a => a.GetName().Name?.Contains("Sqlite", StringComparison.InvariantCulture) == true)
            .ToList();

        var sqlServerMigrations = migrationAssemblies
            .Where(a => a.GetName().Name?.Contains("SqlServer", StringComparison.InvariantCulture) == true)
            .ToList();

        // Assert
        migrationAssemblies.Should().NotBeEmpty("Should have migration assemblies");

        if (sqliteMigrations.Count != 0)
        {
            _output.WriteLine($"Found {sqliteMigrations.Count} SQLite migration assemblies");
        }

        if (sqlServerMigrations.Count != 0)
        {
            _output.WriteLine($"Found {sqlServerMigrations.Count} SQL Server migration assemblies");
        }

        // Verify migrations don't contain business logic
        foreach (var migrationAssembly in migrationAssemblies)
        {
            var businessLogicDependencies = migrationAssembly.GetReferencedAssemblies()
                .Where(a => a.Name?.Contains("Application", StringComparison.InvariantCulture) == true || a.Name?.Contains("Domain", StringComparison.InvariantCulture) == true)
                .Where(a => !a.Name?.Contains("Shared", StringComparison.InvariantCulture) == true) // Shared dependencies are OK
                .ToList();

            businessLogicDependencies.Should().BeEmpty($"Migration assembly {migrationAssembly.GetName().Name} should not depend on business logic");
        }

        _output.WriteLine($"✓ Found {migrationAssemblies.Count} properly separated migration assemblies");
    }

    [Fact]
    public void Shared_Modules_Should_Be_Reusable_Across_Business_Modules()
    {
        // Arrange & Act
        var sharedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.Contains("Shared", StringComparison.InvariantCulture) == true)
            .ToList();

        // Assert
        sharedAssemblies.Should().NotBeEmpty("Should have shared modules");

        foreach (var sharedAssembly in sharedAssemblies)
        {
            // Shared modules should not depend on specific business modules
            var businessDependencies = sharedAssembly.GetReferencedAssemblies()
                .Where(a => a.Name?.Contains("Scorer", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("Referential", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("TeamUp", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("Training", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("Licensing", StringComparison.InvariantCulture) == true)
                .ToList();

            businessDependencies.Should().BeEmpty($"Shared assembly {sharedAssembly.GetName().Name} should not depend on specific business modules");
        }

        _output.WriteLine($"✓ Found {sharedAssemblies.Count} reusable shared modules");
    }

    [Fact]
    public void Module_Boundaries_Should_Be_Respected()
    {
        // Arrange & Act
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("MyClub.", StringComparison.InvariantCulture) == true &&
                       !a.GetName().Name?.Contains("Shared", StringComparison.InvariantCulture) == true &&
                       !a.GetName().Name?.Contains("Tests", StringComparison.InvariantCulture) == true)
            .ToList();

        var moduleNames = moduleAssemblies
            .Select(a => a.GetName().Name?.Split('.')[1]) // Extract module name (Scorer, Referential, etc.)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToList();

        _output.WriteLine($"Found modules: {string.Join(", ", moduleNames)}");

        // Each module should only reference Shared assemblies and not other business modules
        foreach (var assembly in moduleAssemblies)
        {
            var assemblyModuleName = assembly.GetName().Name?.Split('.')[1];
            var referencedModules = assembly.GetReferencedAssemblies()
                .Where(a => a.Name?.StartsWith("MyClub.", StringComparison.InvariantCulture) == true)
                .Select(a => a.Name?.Split('.')[1])
                .Where(name => !string.IsNullOrEmpty(name) &&
                              name != "Shared" &&
                              name != assemblyModuleName &&
                              moduleNames.Contains(name))
                .Distinct()
                .ToList();

            referencedModules.Should().BeEmpty($"Module {assemblyModuleName} should not reference other business modules: {string.Join(", ", referencedModules)}");
        }

        Assert.True(true, $"Module boundary validation completed for {moduleNames.Count} modules");
    }

    [Fact]
    public void CrossCutting_Concerns_Should_Not_Couple_Modules()
    {
        // Arrange & Act
        var crossCuttingAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.Contains("CrossCutting", StringComparison.InvariantCulture) == true);

        var localizationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.Contains("Localization", StringComparison.InvariantCulture) == true);

        // Assert
        if (crossCuttingAssembly != null)
        {
            var businessDependencies = crossCuttingAssembly.GetReferencedAssemblies()
                .Where(a => a.Name?.Contains("Scorer", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("Referential", StringComparison.InvariantCulture) == true)
                .ToList();

            businessDependencies.Should().BeEmpty($"CrossCutting should not depend on specific business modules");
            _output.WriteLine("✓ CrossCutting module is properly decoupled");
        }

        if (localizationAssembly != null)
        {
            var businessDependencies = localizationAssembly.GetReferencedAssemblies()
                .Where(a => a.Name?.Contains("Scorer", StringComparison.InvariantCulture) == true ||
                           a.Name?.Contains("Referential", StringComparison.InvariantCulture) == true)
                .ToList();

            businessDependencies.Should().BeEmpty($"Localization should not depend on specific business modules");
            _output.WriteLine("✓ Localization module is properly decoupled");
        }

        Assert.True(true, "CrossCutting concerns validation completed");
    }

    [Fact]
    public void Infrastructure_Modules_Should_Follow_Modular_Pattern()
    {
        // Force loading of shared infrastructure assembly
        var forceLoad = typeof(Shared.Infrastructure.Persistence.UnitOfWork<>);

        // Arrange & Act
        var infrastructureAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.Contains("Infrastructure", StringComparison.InvariantCulture) == true)
            .ToList();

        var sharedInfrastructure = infrastructureAssemblies
            .Where(a => a.GetName().Name?.Contains("Shared", StringComparison.InvariantCulture) == true)
            .ToList();

        var moduleSpecificInfrastructure = infrastructureAssemblies
            .Where(a => !a.GetName().Name?.Contains("Shared", StringComparison.InvariantCulture) == true)
            .ToList();

        // Assert
        sharedInfrastructure.Should().NotBeEmpty("Should have shared infrastructure");

        // Shared infrastructure should not depend on module-specific infrastructure
        foreach (var sharedInfra in sharedInfrastructure)
        {
            var moduleSpecificDependencies = sharedInfra.GetReferencedAssemblies()
                .Where(a => a.Name?.Contains("Infrastructure", StringComparison.InvariantCulture) == true &&
                           !a.Name?.Contains("Shared", StringComparison.InvariantCulture) == true)
                .ToList();

            moduleSpecificDependencies.Should().BeEmpty($"Shared infrastructure {sharedInfra.GetName().Name} should not depend on module-specific infrastructure");
        }

        _output.WriteLine($"✓ Infrastructure follows modular pattern: {sharedInfrastructure.Count} shared, {moduleSpecificInfrastructure.Count} module-specific");
    }
}
