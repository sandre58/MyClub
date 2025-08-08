// -----------------------------------------------------------------------
// <copyright file="DomainRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Shared.Kernel.Primitives;
using NetArchTest.Rules;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Domain-Driven Design rules and patterns.
/// Ensures proper implementation of DDD concepts like Aggregates, Entities, and Value Objects.
/// </summary>
public class DomainRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(Competition).Assembly;
    private static readonly Assembly SharedDomainAssembly = typeof(Shared.Domain.ValueObjects.DisplayName).Assembly;
    private static readonly Assembly SharedKernelAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void Entities_Should_Inherit_From_Entity_Base_Class()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Aggregate")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("*Id")
            .And()
            .DoNotHaveName("*Repository")
            .Should()
            .Inherit(typeof(Entity<>));

        // Assert
        result.Should().BeSuccessful("Domain entities should inherit from Entity<TId>");
    }

    [Fact]
    public void Aggregates_Should_Not_Reference_Other_Aggregates_Directly()
    {
        // Arrange & Act
        var competitionAggregateResult = Types.InNamespace("MyClub.Scorer.Domain.CompetitionAggregate")
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.MatchAggregate")
            .And()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.StageAggregate")
            .And()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.RoundAggregate");

        var matchAggregateResult = Types.InNamespace("MyClub.Scorer.Domain.MatchAggregate")
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.CompetitionAggregate")
            .And()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.StageAggregate")
            .And()
            .NotHaveDependencyOn("MyClub.Scorer.Domain.RoundAggregate");

        // Assert
        competitionAggregateResult.Should().BeSuccessful("Competition aggregate should not directly reference other aggregates");
        matchAggregateResult.Should().BeSuccessful("Match aggregate should not directly reference other aggregates");
    }

    [Fact]
    public void Repository_Interfaces_Should_Be_In_Domain()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Repositories");

        // Assert
        result.Should().BeSuccessful("Repository interfaces should be defined in Domain layer");
    }

    [Fact]
    public void Value_Objects_Should_Be_Records_Or_Immutable()
    {
        // Note: This test checks that value objects in Shared.Domain are records (immutable by default)
        // Arrange & Act
        var result = Types.InAssembly(SharedDomainAssembly)
            .That()
            .ResideInNamespace("MyClub.Shared.Domain")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeRecords();

        // Assert
        result.Should().BeSuccessful("Value Objects should be implemented as records for immutability");
    }

    [Fact]
    public void Domain_Should_Not_Use_Infrastructure_Concerns()
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
                "Microsoft.AspNetCore"
            );

        // Assert
        result.Should().BeSuccessful("Domain should not depend on infrastructure concerns");
    }

    [Fact]
    public void Domain_Events_Should_Be_In_Domain_Layer()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Event")
            .Should()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Events")
            .Or()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Aggregate");

        // Assert
        result.Should().BeSuccessful("Domain events should be defined in Domain layer");
    }

    [Fact]
    public void Strongly_Typed_Ids_Should_Inherit_From_EntityId()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Id")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(EntityId<>));

        // Assert
        result.Should().BeSuccessful("Strongly-typed IDs should inherit from EntityId<T>");
    }

    [Fact]
    public void Domain_Services_Should_Be_In_Domain_Layer()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Services")
            .Or()
            .ResideInNamespaceMatching("MyClub.Scorer.Domain.*Aggregate");

        // Assert
        result.Should().BeSuccessful("Domain services should be in Domain layer");
    }
}
