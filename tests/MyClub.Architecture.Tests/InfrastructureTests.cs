// -----------------------------------------------------------------------
// <copyright file="InfrastructureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence.Repositories;
using MyClub.Shared.Kernel.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Infrastructure layer patterns and implementations.
/// Ensures proper repository implementations and data access patterns.
/// </summary>
public class InfrastructureTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;

    [Fact]
    public void Repository_Implementations_Should_Be_In_Repositories_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespaceMatching(".*Repositories.*");

        // Assert
        result.Should().BeSuccessful("Repository implementations should be in Repositories namespace");
    }

    [Fact]
    public void Repository_Implementations_Should_Inherit_From_RepositoryBase()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("RepositoryBase")
            .Should()
            .Inherit(typeof(RepositoryBase<,,>));

        // Assert
        result.Should().BeSuccessful("Repository implementations should inherit from RepositoryBase");
    }

    [Fact]
    public void DbContext_Should_Be_In_DbContexts_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.DbContext))
            .Should()
            .ResideInNamespaceMatching(".*DbContexts.*");

        // Assert
        result.Should().BeSuccessful("DbContext should be in DbContexts namespace");
    }

    [Fact]
    public void Entity_Configurations_Should_Be_In_Configuration_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
            .Should()
            .ResideInNamespaceMatching(".*Configuration.*");

        // Assert
        result.Should().BeSuccessful("Entity configurations should be in Configuration namespace");
    }

    [Fact]
    public void Entity_Configurations_Should_Have_Configuration_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
            .Should()
            .HaveNameEndingWith("Configuration");

        // Assert
        result.Should().BeSuccessful("Entity configurations should end with 'Configuration'");
    }

    [Fact]
    public void UnitOfWork_Should_Implement_IUnitOfWork()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("UnitOfWork")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(IUnitOfWork));

        // Assert
        result.Should().BeSuccessful("UnitOfWork implementations should implement IUnitOfWork interface");
    }

    [Fact]
    public void Infrastructure_Should_Only_Depend_On_Domain_And_Shared()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Application");

        // Assert
        result.Should().BeSuccessful("Infrastructure should not depend on Application layer");
    }

    [Fact]
    public void Migrations_Should_Be_In_Migrations_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.Migrations.Migration))
            .Should()
            .ResideInNamespaceMatching(".*Migrations.*");

        // Assert
        result.Should().BeSuccessful("EF Migrations should be in Migrations namespace");
    }

    [Fact]
    public void Repository_Should_Not_Expose_DbContext_Directly()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .Should()
            .NotHavePublicMethods(method => 
                method.ReturnType.FullName != null && 
                method.ReturnType.FullName.Contains("DbContext"));

        // Assert
        result.Should().BeSuccessful("Repositories should not expose DbContext in public methods");
    }
}
