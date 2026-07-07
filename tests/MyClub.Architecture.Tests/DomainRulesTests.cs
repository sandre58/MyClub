// -----------------------------------------------------------------------
// <copyright file="DomainRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Domain-Driven Design rules and patterns.
/// Ensures proper implementation of DDD concepts like Aggregates, Entities, and Value Objects.
/// </summary>
public class DomainRulesTests(ITestOutputHelper output)
{
    private static readonly Assembly DomainAssembly = typeof(Competition).Assembly;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Domain_Architecture_Analysis()
    {
        // Analyze the domain structure
        var allDomainTypes = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Aggregate")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .ToList();

        var entitiesInheritingFromEntity = allDomainTypes
            .Where(isInheritingFromEntity)
            .ToList();

        var otherTypes = allDomainTypes
            .Where(t => !isInheritingFromEntity(t))
            .ToList();

        _output.WriteLine($"Total domain types in aggregates: {allDomainTypes.Count}");
        _output.WriteLine($"Types inheriting from Entity<>: {entitiesInheritingFromEntity.Count}");
        _output.WriteLine($"Other types (likely value objects, IDs, configs): {otherTypes.Count}");

        // Log specific types for analysis
        _output.WriteLine("\nEntities inheriting from Entity<>:", StringComparison.InvariantCulture);
        foreach (var entity in entitiesInheritingFromEntity)
        {
            _output.WriteLine($"  - {entity.Name}");
        }

        _output.WriteLine("\nOther types (value objects, IDs, configurations):", StringComparison.InvariantCulture);
        foreach (var other in otherTypes.Take(10))
        {
            _output.WriteLine($"  - {other.Name} ({getTypeCategory(other)})");
        }

        // Basic assertions
        entitiesInheritingFromEntity.Should().NotBeEmpty("Should have at least some entities inheriting from Entity<>");
        allDomainTypes.Should().NotBeEmpty("Should have domain types");

        static bool isInheritingFromEntity(Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name.Contains("Entity", StringComparison.InvariantCulture))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        static string getTypeCategory(Type type) => type.Name.EndsWith("Id", StringComparison.InvariantCulture)
                ? "ID"
                : type.Name.EndsWith("Reference", StringComparison.InvariantCulture)
                ? "Reference"
                : type.Name.EndsWith("Format", StringComparison.InvariantCulture)
                ? "Configuration"
                : type.Name.EndsWith("Rules", StringComparison.InvariantCulture)
                ? "Configuration"
                : type.Name.EndsWith("Type", StringComparison.InvariantCulture)
                ? "Enum/Type"
                : type.Name.Contains("Label", StringComparison.InvariantCulture) ? "Label" : "Other";
    }

    [Fact]
    public void Main_Aggregate_Roots_Should_Inherit_From_Entity()
    {
        // Test specific main aggregate roots that we know should be entities
        var mainAggregateTypes = new[]
        {
            "Competition", "League", "Cup", "Tournament", "Team", "Match", "Matchday"
        };

        foreach (var typeName in mainAggregateTypes)
        {
            var type = DomainAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName && t.IsClass && !t.IsAbstract);

            if (type != null)
            {
                var inheritsFromEntity = isInheritingFromEntity(type);
                _output.WriteLine($"{typeName}: {(inheritsFromEntity ? "✓ Inherits from Entity" : "✗ Does not inherit from Entity")}");

                if (typeName is "Competition" or "League" or "Cup" or "Tournament")
                {
                    inheritsFromEntity.Should().BeTrue($"{typeName} should inherit from Entity as it's a main aggregate root");
                }
            }
            else
            {
                _output.WriteLine($"{typeName}: Not found in domain assembly");
            }
        }

        static bool isInheritingFromEntity(Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name.Contains("Entity", StringComparison.InvariantCulture))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }
    }

    [Fact]
    public void Repository_Interfaces_Should_Be_In_Domain()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository", StringComparison.InvariantCulture)
            .Should()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Repository interfaces should be in domain namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure_Concerns()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data",
                "System.Net.Http",
                "Microsoft.AspNetCore",
                "Newtonsoft.Json",
                "System.Text.Json")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Domain should not depend on infrastructure concerns. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Layer()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Application")
            .And()
            .NotHaveDependencyOnAny(
                "MediatR",
                "AutoMapper",
                "FluentValidation")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Domain should not depend on application layer. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Services_Should_Be_In_Services_Namespace()
    {
        // Arrange & Act
        var serviceTypes = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Service", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes();

        if (serviceTypes.Any())
        {
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .HaveNameEndingWith("Service", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .Should()
                .ResideInNamespaceMatching(".*Services.*")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Domain services should be in Services namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No domain services found");
        }
    }

    [Fact]
    public void Domain_Events_Should_Implement_IDomainEvent()
    {
        // Arrange & Act
        var eventTypes = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Event", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("MatchEvent") // MatchEvent is an entity, not a domain event
            .GetTypes();

        if (eventTypes.Any())
        {
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .HaveNameEndingWith("Event", StringComparison.InvariantCulture)
                .And()
                .AreClasses()
                .And()
                .DoNotHaveName("MatchEvent") // MatchEvent is an entity, not a domain event
                .Should()
                .ImplementInterface(typeof(MyClub.Shared.Kernel.Events.IDomainEvent))
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"Domain events should implement IDomainEvent. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            Assert.True(true, "No domain events found");
        }
    }

    [Fact]
    public void Strongly_Typed_IDs_Should_Follow_Naming_Convention()
    {
        // Arrange & Act
        var idTypes = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Id", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        // Assert - All ID types should be properly named
        foreach (var idType in idTypes)
        {
            idType.Name.Should().EndWith("Id", $"ID type {idType.Name} should end with 'Id'");

            // Check if it inherits from EntityId (if that's the pattern used)
            var implementsEntityId = idType.BaseType?.IsGenericType == true &&
                                   idType.BaseType.GetGenericTypeDefinition().Name.Contains("EntityId", StringComparison.InvariantCulture);

            Assert.True(implementsEntityId || idType.GetInterfaces().Any(i => i.Name.Contains("EntityId", StringComparison.InvariantCulture)),
                       $"ID type {idType.Name} should inherit from EntityId or implement IEntityId");
        }

        Assert.True(idTypes.Count > 0, "Should have at least one strongly-typed ID");
    }
}
