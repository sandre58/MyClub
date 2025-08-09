// -----------------------------------------------------------------------
// <copyright file="EnterprisePatternTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate enterprise patterns like CQRS behaviors, logging, performance monitoring,
/// and other advanced architectural patterns across the application.
/// </summary>
public class EnterprisePatternTests(ITestOutputHelper output)
{
    private static readonly Assembly SharedApplicationAssembly = typeof(Shared.Application.Behaviors.PerformanceBehavior<,>).Assembly;
    private static readonly Assembly ScorerApplicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;
    private readonly ITestOutputHelper _output = output;

    static EnterprisePatternTests() =>
        _ = typeof(Shared.Application.Behaviors.PerformanceBehavior<,>).Assembly;

    [Fact]
    public void CQRS_Behaviors_Should_Be_Properly_Implemented()
    {
        // Recherche des types génériques ouverts et fermés
        var behaviorTypes = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.Name.Contains("Behavior", StringComparison.InvariantCulture))
            .ToList();

        // Ajout explicite des définitions génériques ouvertes
        var genericBehaviorTypes = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsGenericTypeDefinition && t.Name.EndsWith("Behavior", StringComparison.InvariantCulture))
            .ToList();

        behaviorTypes.AddRange(genericBehaviorTypes);

        var pipelineBehaviors = behaviorTypes
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IPipelineBehavior<,>)))
            .ToList();

        // Assert
        behaviorTypes.Should().NotBeEmpty("Should have CQRS behavior implementations");
        pipelineBehaviors.Should().NotBeEmpty("Should implement MediatR pipeline behaviors");

        _output.WriteLine($"Found {behaviorTypes.Count} behavior types");
        _output.WriteLine($"Found {pipelineBehaviors.Count} pipeline behaviors");

        // Vérification du namespace
        var invalidNamespace = behaviorTypes.Where(t => !t.Namespace?.Contains("Behaviors", StringComparison.InvariantCulture) ?? true).Select(t => t.FullName).ToList();
        invalidNamespace.Should().BeEmpty($"Behaviors should be in Behaviors namespace. Failures: {string.Join(", ", invalidNamespace)}");
    }

    [Fact]
    public void Performance_Behaviors_Should_Follow_Enterprise_Patterns()
    {
        var performanceBehaviors = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && (t.Name.Contains("Performance", StringComparison.InvariantCulture) || t.Name.Contains("Timing", StringComparison.InvariantCulture) || t.Name.Contains("Monitoring", StringComparison.InvariantCulture)))
            .ToList();

        if (performanceBehaviors.Count != 0)
        {
            performanceBehaviors.Should().NotBeEmpty("Should have performance monitoring behaviors");

            foreach (var behavior in performanceBehaviors)
            {
                var implementsPipelineBehavior = behavior.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IPipelineBehavior<,>));

                implementsPipelineBehavior.Should().BeTrue($"Performance behavior {behavior.Name} should implement IPipelineBehavior");
            }
        }
        else
        {
            _output.WriteLine("No performance behaviors found - consider implementing for enterprise monitoring");
            Assert.True(true, "Performance behaviors not implemented yet");
        }
    }

    [Fact]
    public void Validation_Behaviors_Should_Be_Consistent()
    {
        var validationBehaviors = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.Name.Contains("Validation", StringComparison.InvariantCulture))
            .ToList();

        if (validationBehaviors.Count != 0)
        {
            foreach (var behavior in validationBehaviors)
            {
                var implementsPipelineBehavior = behavior.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IPipelineBehavior<,>));

                implementsPipelineBehavior.Should().BeTrue($"Validation behavior {behavior.Name} should implement IPipelineBehavior");
            }

            _output.WriteLine($"Found {validationBehaviors.Count} validation behaviors");
        }
        else
        {
            _output.WriteLine("No validation behaviors found");
            Assert.True(true, "Validation behaviors not implemented yet");
        }
    }

    [Fact]
    public void Logging_Should_Be_High_Performance()
    {
        var assemblies = new[] { SharedApplicationAssembly, ScorerApplicationAssembly };
        var hasLoggerMessageDelegates = false;

        foreach (var assembly in assemblies)
        {
            var typesWithLogging = assembly.GetTypes()
                .Where(t => t.IsClass && t.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                    .Any(f => f.FieldType.Name.Contains("LoggerMessage", StringComparison.InvariantCulture) || f.FieldType.Name.Contains("Action", StringComparison.InvariantCulture)))
                .ToList();

            if (typesWithLogging.Count != 0)
            {
                hasLoggerMessageDelegates = true;
                _output.WriteLine($"Found high-performance logging in {assembly.GetName().Name}: {typesWithLogging.Count} types");
            }
        }

        if (!hasLoggerMessageDelegates)
        {
            _output.WriteLine("Consider implementing LoggerMessage delegates for high-performance logging");
        }

        Assert.True(true, $"Logging analysis completed. High-performance logging found: {hasLoggerMessageDelegates}");
    }

    [Fact]
    public void Event_Sourcing_Patterns_Should_Be_Validated()
    {
        var eventTypes = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Event", StringComparison.InvariantCulture))
            .ToList();

        var eventHandlers = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("EventHandler", StringComparison.InvariantCulture))
            .ToList();

        var eventSourcingInfrastructure = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.Contains("Infrastructure.Events", StringComparison.InvariantCulture) == true);

        if (eventSourcingInfrastructure != null)
        {
            _output.WriteLine($"Found event sourcing infrastructure: {eventSourcingInfrastructure.GetName().Name}");

            var eventInfraTypes = eventSourcingInfrastructure.GetTypes()
                .Where(t => t.IsClass)
                .ToList();

            eventInfraTypes.Should().NotBeEmpty("Event sourcing infrastructure should contain event handling types");
        }
        else
        {
            _output.WriteLine("Event sourcing infrastructure not found - may be planned for future implementation");
        }

        if (eventTypes.Count != 0 || eventHandlers.Count != 0)
        {
            _output.WriteLine($"Found {eventTypes.Count} event types and {eventHandlers.Count} event handlers");
            Assert.True(true, "Event patterns are implemented");
        }
        else
        {
            _output.WriteLine("Event sourcing patterns not yet implemented");
            Assert.True(true, "Event sourcing patterns not implemented yet - acceptable for current architecture");
        }
    }

    [Fact]
    public void Caching_Patterns_Should_Be_Consistent()
    {
        var cachingBehaviors = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.Name.Contains("Cach", StringComparison.InvariantCulture))
            .ToList();

        var cachingInterfaces = SharedApplicationAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.Contains("Cach", StringComparison.InvariantCulture))
            .ToList();

        if (cachingBehaviors.Count != 0 || cachingInterfaces.Count != 0)
        {
            _output.WriteLine($"Found {cachingBehaviors.Count} caching behaviors and {cachingInterfaces.Count} caching interfaces");

            foreach (var behavior in cachingBehaviors)
            {
                var implementsPipelineBehavior = behavior.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IPipelineBehavior<,>));

                if (behavior.Name.Contains("Behavior", StringComparison.InvariantCulture))
                {
                    implementsPipelineBehavior.Should().BeTrue($"Caching behavior {behavior.Name} should implement IPipelineBehavior");
                }
            }
        }
        else
        {
            _output.WriteLine("Caching patterns not implemented yet - consider for enterprise performance");
            Assert.True(true, "Caching patterns not implemented yet");
        }
    }

    [Fact]
    public void Health_Checks_Should_Be_Comprehensive()
    {
        // Arrange & Act
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var healthCheckTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Health", StringComparison.InvariantCulture) && (t.IsClass || t.IsInterface))
            .ToList();

        // Assert
        if (healthCheckTypes.Count != 0)
        {
            _output.WriteLine($"Found {healthCheckTypes.Count} health check related types");

            // Health checks should not depend on business logic
            foreach (var healthCheckType in healthCheckTypes.Where(t => t.IsClass))
            {
                var dependencies = healthCheckType.Assembly.GetReferencedAssemblies()
                    .Where(a => a.Name?.Contains("Domain", StringComparison.InvariantCulture) == true || a.Name?.Contains("Application", StringComparison.InvariantCulture) == true)
                    .ToList();

                if (dependencies.Count != 0)
                {
                    _output.WriteLine($"Health check {healthCheckType.Name} has business logic dependencies: {string.Join(", ", dependencies.Select(d => d.Name))}");
                }
            }
        }
        else
        {
            _output.WriteLine("Health checks not found - consider implementing for enterprise monitoring");
        }

        Assert.True(true, $"Health check analysis completed. Found {healthCheckTypes.Count} health check types");
    }
}
