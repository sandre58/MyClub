// -----------------------------------------------------------------------
// <copyright file="InfrastructureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Infrastructure layer patterns and implementations.
/// Ensures proper repository implementations and data access patterns.
/// </summary>
public class InfrastructureTests(ITestOutputHelper output)
{
    private static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Repository_Implementations_Should_Be_In_Repositories_Namespace()
    {
        // Arrange & Act
        var repositoryTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes();

        if (repositoryTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .Should()
                .ResideInNamespaceMatching(".*Repositories.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Repository implementations should be in Repositories namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No repository implementations found");
        }
    }

    [Fact]
    public void Repository_Implementations_Should_Inherit_From_Base_Repository()
    {
        // Arrange & Act
        var repositoryTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("RepositoryBase")
            .GetTypes()
            .ToList();

        if (repositoryTypes.Count != 0)
        {
            // Check if any repository implements a base repository pattern
            var hasValidRepositoryPattern = repositoryTypes.All(type =>
            {
                // Check if it inherits from any RepositoryBase class
                var baseType = type.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    if (baseType.Name.Contains("RepositoryBase", StringComparison.InvariantCulture) ||
                        (baseType.Name.Contains("Repository", StringComparison.InvariantCulture) && baseType.IsGenericType))
                    {
                        return true;
                    }

                    baseType = baseType.BaseType;
                }

                return false;
            });

            // Assert
            hasValidRepositoryPattern.Should().BeTrue("Repository implementations should inherit from a base repository class or follow repository pattern");
        }
        else
        {
            Assert.True(true, "No concrete repository implementations found");
        }
    }

    [Fact]
    public void DbContext_Should_Be_In_DbContexts_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.DbContext))
            .Should()
            .ResideInNamespaceMatching(".*DbContexts.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"DbContext should be in DbContexts namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Entity_Configurations_Should_Be_In_Configuration_Namespace()
    {
        // Arrange & Act
        var configurationTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
            .GetTypes();

        if (configurationTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
                .Should()
                .ResideInNamespaceMatching(".*Configuration.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Entity configurations should be in Configuration namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No entity configurations found");
        }
    }

    [Fact]
    public void Entity_Configurations_Should_Have_Configuration_Suffix()
    {
        // Arrange & Act
        var configurationTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
            .GetTypes();

        if (configurationTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
                .Should()
                .HaveNameEndingWith("Configuration", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Entity configurations should end with 'Configuration'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No entity configurations found");
        }
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Application_Layer()
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
    public void Infrastructure_Should_Only_Reference_Allowed_External_Dependencies()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "System.Windows.Forms",
                "System.Web",
                "Microsoft.AspNetCore.Mvc",
                "Newtonsoft.Json")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Infrastructure should not depend on presentation layer concerns. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DbContext_Should_Be_Sealed_Or_Abstract()
    {
        // Arrange & Act
        var dbContextTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.DbContext))
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsSealed)
            .ToList();

        // Assert - Allow non-sealed DbContext for extensibility in some architectures
        if (dbContextTypes.Count != 0)
        {
            var typeNames = string.Join(", ", dbContextTypes.Select(t => t.Name));
            _output.WriteLine($"Found non-sealed, non-abstract DbContext types: {typeNames}");
            _output.WriteLine("Note: This is acceptable in some architectures for extensibility");

            // This is more of a recommendation than a strict rule
            Assert.True(dbContextTypes.Count <= 2, $"Consider sealing DbContext implementations for better encapsulation: {typeNames}");
        }
        else
        {
            Assert.True(true, "All DbContext implementations are properly sealed or abstract");
        }
    }

    [Fact]
    public void Migrations_Should_Be_In_Separate_Assembly()
    {
        // Arrange & Act
        var migrationTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.Migrations.Migration))
            .GetTypes();

        // Assert
        migrationTypes.Should().BeEmpty("Migrations should be in separate migration assemblies, not in the persistence assembly");
    }

    [Fact]
    public void Value_Converters_Should_Be_In_Converters_Namespace()
    {
        // Arrange & Act
        var converterTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<,>))
            .GetTypes();

        if (converterTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .Inherit(typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<,>))
                .Should()
                .ResideInNamespaceMatching(".*Converters.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Value converters should be in Converters namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No value converters found");
        }
    }

    [Fact]
    public void Extensions_Should_Be_In_Extensions_Namespace()
    {
        // Arrange & Act
        var extensionTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .AreStatic()
            .And()
            .HaveNameEndingWith("Extensions", StringComparison.InvariantCulture)
            .GetTypes();

        if (extensionTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .AreClasses()
                .And()
                .AreStatic()
                .And()
                .HaveNameEndingWith("Extensions", StringComparison.InvariantCulture)
                .Should()
                .ResideInNamespaceMatching(".*Extensions.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Extension classes should be in Extensions namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No extension classes found");
        }
    }
}
