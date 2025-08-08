// -----------------------------------------------------------------------
// <copyright file="LayerDependencyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
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
    private static readonly Assembly ApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.CreateCompetition.CreateCompetitionCommand).Assembly;
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
            .NotHaveDependencyOn("MyClub.Shared.Application");

        // Assert
        result.Should().BeSuccessful("Domain layer should not depend on Application layer");
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
            .NotHaveDependencyOn("System.Data");

        // Assert
        result.Should().BeSuccessful("Domain layer should not depend on Infrastructure concerns");
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
                "Microsoft.AspNetCore"
            );

        // Assert
        result.Should().BeSuccessful("Domain should only depend on Shared.Domain and Shared.Kernel");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Application()
    {
        // Arrange & Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("MyClub.Scorer.Application");

        // Assert
        result.Should().BeSuccessful("Infrastructure should not depend on Application layer");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
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
    public void Shared_Kernel_Should_Have_No_Dependencies()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MyClub.Scorer",
                "MyClub.Shared.Domain",
                "MyClub.Shared.Application",
                "MyClub.Shared.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "AutoMapper",
                "MediatR"
            );

        // Assert
        result.Should().BeSuccessful("Shared.Kernel should have no external dependencies");
    }
}
