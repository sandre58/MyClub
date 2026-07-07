// -----------------------------------------------------------------------
// <copyright file="SharedInfrastructureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Shared.Application.Abstractions.ErrorHandling;
using MyClub.Shared.Application.Abstractions.Monitoring;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate Shared Infrastructure patterns and enterprise capabilities.
/// Ensures proper implementation of circuit breaker, retry policies, monitoring, and resilience patterns.
/// </summary>
public class SharedInfrastructureTests(ITestOutputHelper output)
{
    private static readonly Assembly SharedInfrastructureAssembly = typeof(IConnectionResilienceService).Assembly;
    private static readonly Assembly SharedApplicationAssembly = typeof(Shared.Application.Commands.CreateCommand).Assembly;
    private static readonly Assembly SharedKernelAssembly = typeof(Shared.Kernel.Primitives.Entity<>).Assembly;

    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Shared_Kernel_Should_Have_No_Business_Logic()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "System.Data",
                "FluentValidation",
                "AutoMapper",
                "Microsoft.AspNetCore")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Shared Kernel should contain only core primitives without business logic dependencies. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Shared_Infrastructure_Should_Contain_Enterprise_Patterns()
    {
        // Arrange & Act - Check for essential enterprise pattern interfaces
        var circuitBreakerTypes = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*CircuitBreaker.*")
            .GetTypes();

        var retryPolicyTypes = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*Retry.*")
            .GetTypes();

        var monitoringTypes = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*Monitoring.*|.*Metrics.*")
            .GetTypes();

        // Assert
        circuitBreakerTypes.Should().NotBeEmpty("Should have circuit breaker pattern implementation");
        retryPolicyTypes.Should().NotBeEmpty("Should have retry policy pattern implementation");
        monitoringTypes.Should().NotBeEmpty("Should have monitoring and metrics capabilities");

        _output.WriteLine($"Found {circuitBreakerTypes.Count()} circuit breaker types");
        _output.WriteLine($"Found {retryPolicyTypes.Count()} retry policy types");
        _output.WriteLine($"Found {monitoringTypes.Count()} monitoring types");
    }

    [Fact]
    public void Circuit_Breaker_Should_Be_Properly_Implemented()
    {
        // Arrange & Act
        var circuitBreakerService = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IConnectionResilienceService))
            .GetTypes();

        var circuitBreakerStates = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*CircuitBreakerState.*")
            .GetTypes();

        // Assert
        circuitBreakerService.Should().NotBeEmpty("Should have circuit breaker service implementation");
        circuitBreakerStates.Should().NotBeEmpty("Should have circuit breaker state management");

        // Verify testable interface exists for testing scenarios
        var testableInterface = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*ITestable.*")
            .GetTypes();

        testableInterface.Should().NotBeEmpty("Should have testable interfaces for circuit breaker testing");
    }

    [Fact]
    public void Retry_Policies_Should_Follow_Enterprise_Patterns()
    {
        // Arrange & Act
        var retryPolicyInterface = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IDatabaseRetryPolicy))
            .GetTypes();

        var retryPolicyImplementations = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameEndingWith("RetryPolicy", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        retryPolicyInterface.Should().NotBeEmpty("Should have retry policy implementations");
        retryPolicyImplementations.Should().NotBeEmpty("Should have concrete retry policy classes");

        // Verify proper naming convention
        foreach (var type in retryPolicyImplementations)
        {
            type.Name.Should().EndWith("RetryPolicy", $"Retry policy {type.Name} should follow naming convention");
        }
    }

    [Fact]
    public void Performance_Monitoring_Should_Be_Consistent()
    {
        // Arrange & Act
        var metricsInterface = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IPersistenceMetrics))
            .GetTypes();

        var monitoringTypes = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ResideInNamespaceMatching(".*Monitoring.*")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        metricsInterface.Should().NotBeEmpty("Should have metrics interface implementations");
        monitoringTypes.Should().NotBeEmpty("Should have monitoring infrastructure");

        // Check for null object pattern implementation
        var nullMetrics = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*Null.*Metrics.*")
            .GetTypes();

        nullMetrics.Should().NotBeEmpty("Should have null object pattern for metrics (for testing/non-production scenarios)");
    }

    [Fact]
    public void Error_Handling_Should_Be_Centralized()
    {
        // Arrange & Act
        var errorHandlers = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ResideInNamespaceMatching(".*ErrorHandling.*")
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        var errorHandlerInterfaces = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .ResideInNamespaceMatching(".*ErrorHandling.*")
            .And()
            .AreInterfaces()
            .GetTypes()
            .ToList();

        // Assert
        errorHandlers.Should().NotBeEmpty("Should have centralized error handling implementations");
        errorHandlerInterfaces.Should().NotBeEmpty("Should have error handling abstractions");

        // Verify error handling is organized by concern
        var connectionErrorTypes = errorHandlers.Where(t => t.Namespace?.Contains("Connection", StringComparison.InvariantCulture) == true).ToList();
        var persistenceErrorTypes = errorHandlers.Where(t => t.Namespace?.Contains("PersistenceError", StringComparison.InvariantCulture) == true).ToList();

        connectionErrorTypes.Should().NotBeEmpty("Should have connection-specific error handling");
        persistenceErrorTypes.Should().NotBeEmpty("Should have persistence-specific error handling");
    }

    [Fact]
    public void Converters_Should_Be_In_Converters_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Converter", StringComparison.InvariantCulture)
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Converters.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Converters should be in Converters namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Repository_Base_Classes_Should_Be_In_Repositories_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*Repository.*")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Repositories.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Repository base classes should be in Repositories namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Conventions_Should_Be_In_Conventions_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedInfrastructureAssembly)
            .That()
            .HaveNameMatching(".*Convention.*")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceMatching(".*Conventions.*")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Database conventions should be in Conventions namespace. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Shared_Application_Should_Only_Contain_Abstractions()
    {
        // Arrange & Act
        var result = Types.InAssembly(SharedApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue($"Shared Application should not depend on specific database implementations. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Cross_Cutting_Concerns_Should_Be_Interface_Only()
    {
        // Find CrossCutting assembly if it exists
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var crossCuttingAssembly = assemblies.FirstOrDefault(a => a.GetName().Name?.Contains("CrossCutting", StringComparison.InvariantCulture) == true);

        if (crossCuttingAssembly != null)
        {
            // Arrange & Act
            var result = Types.InAssembly(crossCuttingAssembly)
                .Should()
                .NotHaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "MediatR",
                    "AutoMapper",
                    "FluentValidation")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue($"CrossCutting should contain only interfaces and abstractions. Failures: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
        else
        {
            _output.WriteLine("CrossCutting assembly not found - test skipped");
            Assert.True(true, "CrossCutting assembly not loaded");
        }
    }
}
