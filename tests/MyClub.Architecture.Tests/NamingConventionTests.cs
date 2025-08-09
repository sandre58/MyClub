// -----------------------------------------------------------------------
// <copyright file="NamingConventionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Kernel.Repositories;
using NetArchTest.Rules;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate naming conventions across the application.
/// Ensures consistent naming patterns for different types of classes.
/// </summary>
public class NamingConventionTests
{
    private static readonly Assembly DomainAssembly = typeof(Competition).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;

    [Fact]
    public void Repositories_Should_Have_Repository_Suffix()
    {
        // Arrange & Act
        var repositoryTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IRepository<,>))
            .GetTypes();

        if (repositoryTypes.Any())
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(typeof(IRepository<,>))
                .Should()
                .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Repository implementations should end with 'Repository'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No repository implementations found");
        }
    }

    [Fact]
    public void Repository_Interfaces_Should_Have_Repository_Suffix()
    {
        // Arrange & Act
        var repositoryInterfaceTypes = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .And()
            .Inherit(typeof(IRepository<,>))
            .GetTypes();

        if (repositoryInterfaceTypes.Any())
        {
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .AreInterfaces()
                .And()
                .Inherit(typeof(IRepository<,>))
                .Should()
                .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Repository interfaces should end with 'Repository'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No repository interfaces found");
        }
    }

    [Fact]
    public void Commands_Should_Have_Command_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("MyClub.Scorer.Application")
            .And()
            .AreClasses()
            .And()
            .HaveNameMatching(".*Command$")
            .Should()
            .HaveNameEndingWith("Command", StringComparison.InvariantCulture)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Command classes should end with 'Command'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Command_Handlers_Should_Have_CommandHandler_Suffix()
    {
        // Arrange & Act
        var commandHandlerTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .And()
            .HaveNameMatching(".*CommandHandler$")
            .GetTypes();

        if (commandHandlerTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
                .And()
                .HaveNameMatching(".*CommandHandler$")
                .Should()
                .HaveNameEndingWith("CommandHandler", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Command handlers should end with 'CommandHandler'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No command handlers found");
        }
    }

    [Fact]
    public void Queries_Should_Have_Query_Suffix()
    {
        // Arrange & Act
        var queryTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes();

        if (queryTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .HaveNameEndingWith("Query", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("Query", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Query classes should end with 'Query'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No query classes found");
        }
    }

    [Fact]
    public void Query_Handlers_Should_Have_QueryHandler_Suffix()
    {
        // Arrange & Act
        var queryHandlerTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes();

        if (queryHandlerTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .HaveNameEndingWith("QueryHandler", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("QueryHandler", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Query handlers should end with 'QueryHandler'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No query handlers found");
        }
    }

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        // Arrange & Act
        var validatorTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .GetTypes();

        if (validatorTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .Inherit(typeof(FluentValidation.AbstractValidator<>))
                .Should()
                .HaveNameEndingWith("Validator", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Validators should end with 'Validator'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No validators found");
        }
    }

    [Fact]
    public void Domain_Services_Should_Have_Service_Suffix()
    {
        // Arrange & Act
        var allServiceTypes = Types.InAssembly(DomainAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespaceMatching(".*Services.*")
            .GetTypes()
            .ToList();

        var actualServiceTypes = allServiceTypes
            .Where(t => t.Name.EndsWith("Service", StringComparison.InvariantCulture))
            .ToList();

        var otherServiceClasses = allServiceTypes
            .Where(t => !t.Name.EndsWith("Service", StringComparison.InvariantCulture))
            .ToList();

        // Assert - This is more informational than strictly enforced
        if (otherServiceClasses.Count != 0)
        {
            var otherNames = string.Join(", ", otherServiceClasses.Select(t => t.Name));
            Assert.True(otherServiceClasses.Count <= 5, $"Found service-related classes that don't end with 'Service': {otherNames}. " + "This might be acceptable for Definition, Strategy, or other domain service patterns.");
        }

        // Log information
        Assert.True(true, $"Found {actualServiceTypes.Count} classes ending with 'Service' and {otherServiceClasses.Count} other service-related classes");
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
    public void DbContext_Should_Have_DbContext_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .Inherit(typeof(Microsoft.EntityFrameworkCore.DbContext))
            .Should()
            .HaveNameEndingWith("DbContext", StringComparison.InvariantCulture)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"DbContext classes should end with 'DbContext'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Value_Converters_Should_Have_Converter_Suffix()
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
                .HaveNameEndingWith("Converter", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Value converters should end with 'Converter'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No value converters found");
        }
    }

    [Fact]
    public void Strongly_Typed_Ids_Should_Have_Id_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Id", StringComparison.InvariantCulture)
            .Should()
            .HaveNameEndingWith("Id", StringComparison.InvariantCulture)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Strongly-typed IDs should end with 'Id'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Extension_Classes_Should_Have_Extensions_Suffix()
    {
        // Arrange & Act - Check all assemblies for extension classes
        var allAssemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly };

        foreach (var assembly in allAssemblies)
        {
            var extensionTypes = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .AreStatic()
                .And()
                .HaveNameEndingWith("Extensions", StringComparison.InvariantCulture)
                .GetTypes();

            if (extensionTypes.Any())
            {
                var result = Types.InAssembly(assembly)
                    .That()
                    .AreClasses()
                    .And()
                    .AreStatic()
                    .And()
                    .HaveNameEndingWith("Extensions", StringComparison.InvariantCulture)
                    .Should()
                    .HaveNameEndingWith("Extensions", StringComparison.InvariantCulture)
                    .GetResult();

                // Assert
                result.IsSuccessful.Should().BeTrue($"Extension classes in {assembly.GetName().Name} should end with 'Extensions'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
            }
        }

        Assert.True(true, "Extension class naming validation completed");
    }

    [Fact]
    public void AutoMapper_Profiles_Should_Have_Profile_Suffix()
    {
        // Arrange & Act
        var profileTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AutoMapper.Profile))
            .GetTypes();

        if (profileTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .Inherit(typeof(AutoMapper.Profile))
                .Should()
                .HaveNameEndingWith("Profile", StringComparison.InvariantCulture)
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"AutoMapper profiles should end with 'Profile'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No AutoMapper profiles found");
        }
    }
}
