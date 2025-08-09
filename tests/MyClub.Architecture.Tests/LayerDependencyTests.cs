// -----------------------------------------------------------------------
// <copyright file="LayerDependencyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Kernel.Primitives;
using NetArchTest.Rules;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate that layer dependencies respect Clean Architecture principles.
/// Ensures that inner layers do not depend on outer layers.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Competition).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;
    private static readonly Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Application")
            .And()
            .NotHaveDependencyOn("MyClub.Shared.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Domain layer should not depend on Application layer. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Infrastructure")
            .And()
            .NotHaveDependencyOn("MyClub.Shared.Infrastructure")
            .And()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .And()
            .NotHaveDependencyOn("System.Data")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Domain layer should not depend on Infrastructure concerns. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Only_Depend_On_Shared_Domain_And_Kernel()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "AutoMapper",
                "MediatR",
                "FluentValidation",
                "Microsoft.AspNetCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Domain should only depend on Shared.Domain and Shared.Kernel. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Application()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Infrastructure should not depend on Application layer. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Implementations()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Infrastructure.Persistence")
            .And()
            .NotHaveDependencyOn("MyClub.Scorer.Infrastructure.Migrations")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Application should not depend on Infrastructure implementations. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Can_Depend_On_Domain()
    {
        // Arrange & Act
        var hasdomainDependency = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Domain")
            .GetTypes()
            .Any();

        // Assert
        hasdomainDependency.Should().BeTrue("Application layer should depend on Domain layer");
    }

    [Fact]
    public void Infrastructure_Can_Depend_On_Domain()
    {
        // Arrange & Act
        var hasDomainDependency = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Domain")
            .GetTypes()
            .Any();

        // Assert
        hasDomainDependency.Should().BeTrue("Infrastructure layer should depend on Domain layer");
    }

    [Fact]
    public void No_Circular_Dependencies_Between_Assemblies()
    {
        // This test verifies that there are no circular dependencies
        // by checking that if A depends on B, then B should not depend on A

        // Check Domain -> Application (should not exist)
        var domainToApp = Types.InAssembly(DomainAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Application")
            .GetTypes();

        domainToApp.Should().BeEmpty("Domain should not depend on Application (circular dependency)");

        // Check Domain -> Infrastructure (should not exist)
        var domainToInfra = Types.InAssembly(DomainAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Infrastructure")
            .GetTypes();

        domainToInfra.Should().BeEmpty("Domain should not depend on Infrastructure (circular dependency)");

        // Check Infrastructure -> Application (should not exist)
        var infraToApp = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Application")
            .GetTypes();

        infraToApp.Should().BeEmpty("Infrastructure should not depend on Application (circular dependency)");
    }

    [Fact]
    public void Shared_Kernel_Should_Have_No_External_Dependencies()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MyClub.Scorer",
                "Microsoft.EntityFrameworkCore",
                "AutoMapper",
                "MediatR",
                "FluentValidation",
                "Microsoft.AspNetCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Shared Kernel should have minimal external dependencies. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void All_Assemblies_Should_Follow_Dependency_Direction()
    {
        // Test the overall dependency flow: UI -> Application -> Domain -> Shared
        // Infrastructure can depend on Domain but not on Application

        // Application should depend on Domain
        var appToDomain = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Domain")
            .GetTypes();

        appToDomain.Should().NotBeEmpty("Application should depend on Domain");

        // Application should depend on Shared
        var appToShared = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveDependencyOn("MyClub.Shared")
            .GetTypes();

        appToShared.Should().NotBeEmpty("Application should depend on Shared assemblies");

        // Infrastructure should depend on Domain
        var infraToDomain = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn("MyClub.Scorer.Domain")
            .GetTypes();

        infraToDomain.Should().NotBeEmpty("Infrastructure should depend on Domain");
    }

    [Fact]
    public void Assemblies_Should_Not_Have_Transitive_Dependency_Violations()
    {
        // Verify that dependencies don't create indirect violations
        // E.g., if Domain depends on SharedX and SharedX depends on Infrastructure,
        // that would be an indirect violation
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "System.Data.Common",
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data",
                "Microsoft.EntityFrameworkCore.SqlServer",
                "Microsoft.EntityFrameworkCore.Sqlite",
                "Microsoft.EntityFrameworkCore.InMemory")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"Domain should not have transitive dependencies on infrastructure libraries. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
