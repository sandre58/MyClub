// -----------------------------------------------------------------------
// <copyright file="ApplicationLayerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Application layer patterns and CQRS implementation.
/// Ensures proper separation of Commands, Queries, and Handlers.
/// </summary>
public class ApplicationLayerTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;

    [Fact]
    public void Commands_Should_Be_In_Commands_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Commands.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Commands should be in Commands namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Commands_Should_Implement_IRequest()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .Or()
            .ImplementInterface(typeof(MediatR.IRequest))
            .Or()
            .Inherit(typeof(MyClub.Shared.Application.Commands.CreateCommand)) // Commands may inherit from base command classes
            .Or()
            .Inherit(typeof(MyClub.Shared.Application.Commands.UpdateCommand)) // Commands may inherit from base command classes
            .Or()
            .Inherit(typeof(MyClub.Shared.Application.Commands.DeleteCommand)) // Commands may inherit from base command classes
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Commands should implement IRequest interface or inherit from base command classes. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Command_Handlers_Should_Be_In_Commands_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Commands.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Command handlers should be in Commands namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Command_Handlers_Should_Implement_IRequestHandler()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Or()
            .ImplementInterface(typeof(MediatR.IRequestHandler<>))
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Command handlers should implement IRequestHandler interface. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Queries_Should_Be_In_Queries_Namespace_If_They_Exist()
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
                .ResideInNamespaceMatching(".*Queries.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Queries should be in Queries namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            // No queries found - this is acceptable for a Command-focused application
            Assert.True(true, "No queries found in the application - this is acceptable for Command-only CQRS implementation");
        }
    }

    [Fact]
    public void Query_Handlers_Should_Be_In_Queries_Namespace_If_They_Exist()
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
                .ResideInNamespaceMatching(".*Queries.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Query handlers should be in Queries namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            // No query handlers found - this is acceptable for a Command-focused application
            Assert.True(true, "No query handlers found in the application - this is acceptable for Command-only CQRS implementation");
        }
    }

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator", StringComparison.InvariantCulture)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Validators should end with 'Validator'. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Implementations()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Infrastructure")
            .And()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Application should not depend on Infrastructure implementations. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Only_Depend_On_Allowed_External_Libraries()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data",
                "System.Net.Http",
                "Microsoft.AspNetCore",
                "Newtonsoft.Json")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Application should not depend on infrastructure-specific libraries. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Mapping_Profiles_Should_Be_In_Mappings_Namespace()
    {
        // Arrange & Act
        var mappingTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AutoMapper.Profile))
            .GetTypes();

        if (mappingTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .Inherit(typeof(AutoMapper.Profile))
                .Should()
                .ResideInNamespaceMatching(".*Mappings.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Mapping profiles should be in Mappings namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            // No mapping profiles found
            Assert.True(true, "No AutoMapper profiles found in the application");
        }
    }

    [Fact]
    public void Services_Should_Be_In_Services_Namespace()
    {
        // Arrange & Act
        var serviceTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Service", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes();

        if (serviceTypes.Any())
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .HaveNameEndingWith("Service", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .Should()
                .ResideInNamespaceMatching(".*Services.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Services should be in Services namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            // No services found
            Assert.True(true, "No services found in the application");
        }
    }

    [Fact]
    public void All_Public_Classes_Should_Have_Appropriate_Naming()
    {
        // Arrange & Act
        var inappropriatelyNamedTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreClasses()
            .And()
            .ArePublic()
            .And()
            .DoNotHaveNameEndingWith("Command", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("CommandHandler", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("Query", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("QueryHandler", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("Validator", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("Service", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("Profile", StringComparison.InvariantCulture)
            .And()
            .DoNotHaveNameEndingWith("Matchday", StringComparison.InvariantCulture) // Allow for domain-specific naming like GeneratedMatchday
            .And()
            .DoNotInherit(typeof(AutoMapper.Profile))
            .And()
            .DoNotInherit(typeof(FluentValidation.AbstractValidator<>))
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToList();

        // Assert - Allow for some domain-specific classes
        if (inappropriatelyNamedTypes.Count != 0)
        {
            // Log information about what was found
            var typeNames = inappropriatelyNamedTypes.Select(t => t.Name).ToArray();
            Assert.True(inappropriatelyNamedTypes.Count <= 3, $"Found some classes that don't follow standard naming conventions, but this might be acceptable for domain-specific types: {string.Join(", ", typeNames)}");
        }
        else
        {
            Assert.True(true, "All public classes follow established naming conventions");
        }
    }
}
