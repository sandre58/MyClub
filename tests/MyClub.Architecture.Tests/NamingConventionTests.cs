// -----------------------------------------------------------------------
// <copyright file="NamingConventionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
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
    private static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.CreateCompetition.CreateCompetitionCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ScorerDbContext).Assembly;

    [Fact]
    public void Repositories_Should_Have_Repository_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IRepository<,>))
            .Should()
            .HaveNameEndingWith("Repository");

        // Assert
        result.Should().BeSuccessful("Repository implementations should end with 'Repository'");
    }

    [Fact]
    public void Repository_Interfaces_Should_Have_Repository_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .And()
            .Inherit(typeof(IRepository<,>))
            .Should()
            .HaveNameEndingWith("Repository");

        // Assert
        result.Should().BeSuccessful("Repository interfaces should end with 'Repository'");
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
            .HaveNameEndingWith("Command");

        // Assert
        result.Should().BeSuccessful("Command classes should end with 'Command'");
    }

    [Fact]
    public void Command_Handlers_Should_Have_CommandHandler_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .And()
            .HaveNameMatching(".*CommandHandler$")
            .Should()
            .HaveNameEndingWith("CommandHandler");

        // Assert
        result.Should().BeSuccessful("Command handlers should end with 'CommandHandler'");
    }

    [Fact]
    public void Queries_Should_Have_Query_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("MyClub.Scorer.Application")
            .And()
            .AreClasses()
            .And()
            .HaveNameMatching(".*Query$")
            .Should()
            .HaveNameEndingWith("Query");

        // Assert
        result.Should().BeSuccessful("Query classes should end with 'Query'");
    }

    [Fact]
    public void Query_Handlers_Should_Have_QueryHandler_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .And()
            .HaveNameMatching(".*QueryHandler$")
            .Should()
            .HaveNameEndingWith("QueryHandler");

        // Assert
        result.Should().BeSuccessful("Query handlers should end with 'QueryHandler'");
    }

    [Fact]
    public void Domain_Services_Should_Have_Service_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("MyClub.Scorer.Domain")
            .And()
            .AreClasses()
            .And()
            .HaveNameMatching(".*Service$")
            .Should()
            .HaveNameEndingWith("Service");

        // Assert
        result.Should().BeSuccessful("Domain services should end with 'Service'");
    }

    [Fact]
    public void Value_Objects_Should_Not_Have_Entity_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("MyClub.Shared.Domain")
            .And()
            .AreClasses()
            .Should()
            .NotHaveNameEndingWith("Entity");

        // Assert
        result.Should().BeSuccessful("Value Objects should not end with 'Entity'");
    }
}
