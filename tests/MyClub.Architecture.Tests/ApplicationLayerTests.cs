// -----------------------------------------------------------------------
// <copyright file="ApplicationLayerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
            .HaveNameEndingWith("Command")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Commands.*");

        // Assert
        result.Should().BeSuccessful("Commands should be in Commands namespace");
    }

    [Fact]
    public void Queries_Should_Be_In_Queries_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Queries.*");

        // Assert
        result.Should().BeSuccessful("Queries should be in Queries namespace");
    }

    [Fact]
    public void Command_Handlers_Should_Be_In_Commands_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Commands.*");

        // Assert
        result.Should().BeSuccessful("Command handlers should be in Commands namespace");
    }

    [Fact]
    public void Query_Handlers_Should_Be_In_Queries_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Queries.*");

        // Assert
        result.Should().BeSuccessful("Query handlers should be in Queries namespace");
    }

    [Fact]
    public void Handlers_Should_Implement_IRequestHandler()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Or()
            .ImplementInterface(typeof(MediatR.IRequestHandler<>));

        // Assert
        result.Should().BeSuccessful("Handlers should implement IRequestHandler interface");
    }

    [Fact]
    public void Commands_Should_Implement_IRequest()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .Or()
            .ImplementInterface(typeof(MediatR.IRequest));

        // Assert
        result.Should().BeSuccessful("Commands should implement IRequest interface");
    }

    [Fact]
    public void Queries_Should_Implement_IRequest()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(MediatR.IRequest<>));

        // Assert
        result.Should().BeSuccessful("Queries should implement IRequest<T> interface");
    }

    [Fact]
    public void Validators_Should_Have_Validator_Suffix()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator");

        // Assert
        result.Should().BeSuccessful("Validators should end with 'Validator'");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Implementations()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Infrastructure")
            .And()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore");

        // Assert
        result.Should().BeSuccessful("Application should not depend on Infrastructure implementations");
    }

    [Fact]
    public void DTOs_Should_Be_In_DTOs_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Dto")
            .Or()
            .HaveNameEndingWith("DTO")
            .Should()
            .ResideInNamespaceMatching(".*DTOs.*")
            .Or()
            .ResideInNamespaceMatching(".*Dto.*");

        // Assert
        result.Should().BeSuccessful("DTOs should be in DTOs namespace");
    }
}
