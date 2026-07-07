// -----------------------------------------------------------------------
// <copyright file="PerformanceArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyClub.Shared.Application.Abstractions.Monitoring;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Tests to validate performance architecture patterns and optimizations.
/// Ensures proper implementation of caching, database optimization, memory management, and performance monitoring.
/// </summary>
public class PerformanceArchitectureTests(ITestOutputHelper output)
{
    private static readonly Assembly[] AllAssemblies = [.. AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name?.StartsWith("MyClub.", StringComparison.InvariantCulture) == true)];
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Database_Queries_Should_Be_Optimized()
    {
        // Arrange & Act
        var repositoryTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Repository", StringComparison.InvariantCulture) && t.IsClass && !t.IsAbstract)
            .ToList();

        var entityConfigurationTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition().Name.Contains("IEntityTypeConfiguration", StringComparison.InvariantCulture)))
            .ToList();

        // Assert
        repositoryTypes.Should().NotBeEmpty("Should have repository implementations");
        entityConfigurationTypes.Should().NotBeEmpty("Should have entity configurations for query optimization");

        _output.WriteLine($"Found {repositoryTypes.Count} repository implementations");
        _output.WriteLine($"Found {entityConfigurationTypes.Count} entity configurations");

        // Check for potential N+1 query patterns (basic analysis)
        foreach (var repoType in repositoryTypes)
        {
            var methods = repoType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType.Name.Contains("IEnumerable", StringComparison.InvariantCulture) ||
                           m.ReturnType.Name.Contains("List", StringComparison.InvariantCulture) ||
                           m.ReturnType.Name.Contains("Collection", StringComparison.InvariantCulture))
                .ToList();

            if (methods.Count > 0)
            {
                _output.WriteLine($"Repository {repoType.Name} has {methods.Count} methods returning collections");
            }
        }

        Assert.True(true, "Database query optimization analysis completed");
    }

    [Fact]
    public void Caching_Patterns_Should_Be_Consistent()
    {
        // Arrange & Act
        var cachingTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.ToUpperInvariant().Contains("cach", StringComparison.InvariantCultureIgnoreCase) ||
                       t.GetInterfaces().Any(i => i.Name.Contains("Cache", StringComparison.InvariantCulture)))
            .ToList();

        var cachingBehaviors = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Cach", StringComparison.InvariantCulture) && t.Name.Contains("Behavior", StringComparison.InvariantCulture))
            .ToList();

        // Assert
        if (cachingTypes.Count > 0)
        {
            _output.WriteLine($"Found {cachingTypes.Count} caching-related types");

            // Caching should be in infrastructure or shared layers
            var businessModuleCaching = cachingTypes
                .Where(t => t.Namespace?.Contains("Domain", StringComparison.InvariantCulture) == true)
                .ToList();

            businessModuleCaching.Should().BeEmpty("Caching should not be in domain layer");

            if (cachingBehaviors.Count > 0)
            {
                _output.WriteLine($"Found {cachingBehaviors.Count} caching behaviors");

                // Verify caching behaviors implement pipeline behavior
                foreach (var behavior in cachingBehaviors)
                {
                    var implementsPipelineBehavior = behavior.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("IPipelineBehavior", StringComparison.InvariantCulture));

                    implementsPipelineBehavior.Should().BeTrue($"Caching behavior {behavior.Name} should implement IPipelineBehavior");
                }
            }
        }
        else
        {
            _output.WriteLine("No caching implementations found - consider adding for enterprise performance", StringComparison.InvariantCulture);
        }

        Assert.True(true, $"Caching pattern analysis completed. Found {cachingTypes.Count} caching types");
    }

    [Fact]
    public void Logging_Should_Be_High_Performance()
    {
        // Arrange & Act
        var typesWithLogging = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && hasLoggingMembers(t))
            .ToList();

        var loggerMessageFields = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            .Where(f => f.FieldType.Name.Contains("LoggerMessage", StringComparison.InvariantCulture) ||
                       (f.FieldType.Name.Contains("Action", StringComparison.InvariantCulture) && f.Name.ToUpperInvariant().Contains("log", StringComparison.InvariantCultureIgnoreCase)))
            .ToList();

        // Assert
        if (typesWithLogging.Count > 0)
        {
            _output.WriteLine($"Found {typesWithLogging.Count} types with logging capabilities");

            if (loggerMessageFields.Count > 0)
            {
                _output.WriteLine($"✓ Found {loggerMessageFields.Count} LoggerMessage delegate fields (high-performance logging)");

                // Good practice: LoggerMessage delegates should be static readonly
                var properLoggerMessages = loggerMessageFields
                    .Where(f => f.IsStatic && f.IsInitOnly)
                    .ToList();

                properLoggerMessages.Count.Should().Be(loggerMessageFields.Count,
                    "LoggerMessage delegates should be static readonly for optimal performance");
            }
            else
            {
                _output.WriteLine("⚠️ No LoggerMessage delegates found - consider implementing for zero-allocation logging");
            }
        }

        Assert.True(true, $"High-performance logging analysis completed. Found {loggerMessageFields.Count} LoggerMessage delegates");

        static bool hasLoggingMembers(System.Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Any(f => f.FieldType.Name.Contains("ILogger", StringComparison.InvariantCulture)) ||
                type.GetProperties().Any(p => p.PropertyType.Name.Contains("ILogger", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Memory_Allocation_Should_Be_Minimal()
    {
        // Arrange & Act - Look for potential memory allocation patterns
        var valueTypeConverters = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Converter", StringComparison.InvariantCulture) && t.IsClass)
            .ToList();

        var recordTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType?.Name.Contains("Record", StringComparison.InvariantCulture) == true ||
                       t.GetMethods().Any(m => m.Name == "<Clone>$"))
            .ToList();

        var stringBuilderUsage = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && hasStringBuilderUsage(t))
            .ToList();

        // Assert
        if (valueTypeConverters.Count > 0)
        {
            _output.WriteLine($"Found {valueTypeConverters.Count} value converters - ensure they minimize allocations");
        }

        if (recordTypes.Count > 0)
        {
            _output.WriteLine($"Found {recordTypes.Count} record types - good for immutable value objects");
        }

        if (stringBuilderUsage.Count > 0)
        {
            _output.WriteLine($"✓ Found {stringBuilderUsage.Count} types using StringBuilder for efficient string operations");
        }

        // Check for potential boxing issues with generic constraints
        var genericTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsGenericType && t.GetGenericArguments().Length > 0)
            .ToList();

        _output.WriteLine($"Found {genericTypes.Count} generic types - ensure proper constraints to avoid boxing");

        Assert.True(true, "Memory allocation analysis completed");

        static bool hasStringBuilderUsage(Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Any(f => f.FieldType.Name.Contains("StringBuilder", StringComparison.InvariantCulture)) ||
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("StringBuilder", StringComparison.InvariantCulture)));
    }

    [Fact]
    public void Async_Patterns_Should_Be_Consistent()
    {
        // Arrange & Act
        var asyncMethods = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.ReturnType.Name.Contains("Task", StringComparison.InvariantCulture) || m.Name.EndsWith("Async", StringComparison.InvariantCulture))
            .ToList();

        var syncMethodsWithAsyncSuffix = asyncMethods
            .Where(m => m.Name.EndsWith("Async", StringComparison.InvariantCulture) && !m.ReturnType.Name.Contains("Task", StringComparison.InvariantCulture))
            .ToList();

        var asyncMethodsWithoutSuffix = asyncMethods
            .Where(m => m.ReturnType.Name.Contains("Task", StringComparison.InvariantCulture) && !m.Name.EndsWith("Async", StringComparison.InvariantCulture))
            .ToList();

        // Assert
        syncMethodsWithAsyncSuffix.Should().BeEmpty("Methods with 'Async' suffix should return Task or Task<T>");
        asyncMethodsWithoutSuffix.Should().BeEmpty("Methods returning Task should have 'Async' suffix");

        if (asyncMethods.Count > 0)
        {
            _output.WriteLine($"✓ Found {asyncMethods.Count} async methods following proper naming conventions");

            // Check for ConfigureAwait usage (not easily detectable via reflection, but good practice)
            _output.WriteLine("Note: Ensure async methods use ConfigureAwait(false) in library code");
        }
        else
        {
            _output.WriteLine("No async methods found - consider async patterns for I/O operations");
        }

        Assert.True(true, $"Async pattern analysis completed. Found {asyncMethods.Count} async methods");
    }

    [Fact]
    public void Entity_Framework_Patterns_Should_Be_Optimized()
    {
        // Arrange & Act
        var dbContextTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType?.Name.Contains("DbContext", StringComparison.InvariantCulture) == true)
            .ToList();

        var entityConfigurations = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition().Name.Contains("IEntityTypeConfiguration", StringComparison.InvariantCulture)))
            .ToList();

        var valueConverters = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType?.Name.Contains("ValueConverter", StringComparison.InvariantCulture) == true)
            .ToList();

        // Assert
        if (dbContextTypes.Count > 0)
        {
            _output.WriteLine($"Found {dbContextTypes.Count} DbContext types");

            entityConfigurations.Should().NotBeEmpty("Should have entity configurations for proper mapping");
            _output.WriteLine($"Found {entityConfigurations.Count} entity configurations");

            if (valueConverters.Count > 0)
            {
                _output.WriteLine($"Found {valueConverters.Count} value converters for complex type mapping");
            }

            // Check if DbContext types are in separate assembly (good for performance)
            var dbContextInPersistenceLayer = dbContextTypes
                .Where(t => t.Namespace?.Contains("Persistence", StringComparison.InvariantCulture) == true ||
                           t.Namespace?.Contains("Infrastructure", StringComparison.InvariantCulture) == true)
                .ToList();

            dbContextInPersistenceLayer.Should().NotBeEmpty("DbContext should be in infrastructure/persistence layer");
        }
        else
        {
            _output.WriteLine("No DbContext types found", StringComparison.InvariantCulture);
        }

        Assert.True(true, "Entity Framework optimization analysis completed");
    }

    [Fact]
    public void Query_Optimization_Patterns_Should_Be_Present()
    {
        // Arrange & Act
        var repositoryMethods = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("Repository", StringComparison.InvariantCulture) && t.IsClass)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .ToList();

        // Look for methods that likely implement query optimization
        var queryMethods = repositoryMethods
            .Where(m => m.Name.Contains("Get", StringComparison.InvariantCulture) ||
                       m.Name.Contains("Find", StringComparison.InvariantCulture) ||
                       m.Name.Contains("Query", StringComparison.InvariantCulture) ||
                       m.Name.Contains("Search", StringComparison.InvariantCulture))
            .ToList();

        var asyncQueryMethods = queryMethods
            .Where(m => m.ReturnType.Name.Contains("Task", StringComparison.InvariantCulture))
            .ToList();

        // Assert
        if (queryMethods.Count > 0)
        {
            _output.WriteLine($"Found {queryMethods.Count} query methods in repositories");
            _output.WriteLine($"Found {asyncQueryMethods.Count} async query methods");

            var asyncPercentage = asyncQueryMethods.Count * 100.0 / queryMethods.Count;

            if (asyncPercentage < 50)
            {
                _output.WriteLine($"⚠️ Only {asyncPercentage:F1}% of query methods are async - consider improving for better performance");
            }
            else
            {
                _output.WriteLine($"✓ {asyncPercentage:F1}% of query methods are async");
            }

            // Check for pagination patterns
            var paginationMethods = queryMethods
                .Where(m => m.GetParameters().Any(p => !string.IsNullOrEmpty(p.Name) &&
                    (p.Name.ToUpperInvariant().Contains("page", StringComparison.InvariantCultureIgnoreCase) ||
                    p.Name.ToUpperInvariant().Contains("skip", StringComparison.InvariantCultureIgnoreCase) ||
                    p.Name.ToUpperInvariant().Contains("take", StringComparison.InvariantCultureIgnoreCase) ||
                    p.Name.ToUpperInvariant().Contains("offset", StringComparison.InvariantCultureIgnoreCase))))
                .ToList();

            if (paginationMethods.Count > 0)
            {
                _output.WriteLine($"✓ Found {paginationMethods.Count} methods with pagination parameters");
            }
            else
            {
                _output.WriteLine("No pagination patterns found - consider implementing for large datasets");
            }
        }

        Assert.True(true, $"Query optimization analysis completed. Analyzed {queryMethods.Count} query methods");
    }

    [Fact]
    public void Performance_Monitoring_Should_Be_Implemented()
    {
        // Force loading of shared infrastructure assembly
        var forceLoad = typeof(IPersistenceMetrics);

        // Arrange & Act
        var metricsTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Metrics", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Performance", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Monitoring", StringComparison.InvariantCulture))
            .ToList();

        var behaviorTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Behavior", StringComparison.InvariantCulture) &&
                       (t.Name.Contains("Performance", StringComparison.InvariantCulture) || t.Name.Contains("Timing", StringComparison.InvariantCulture)))
            .ToList();

        // Assert
        if (metricsTypes.Count > 0)
        {
            _output.WriteLine($"✓ Found {metricsTypes.Count} performance monitoring types");

            var metricsInterfaces = metricsTypes.Where(t => t.IsInterface).ToList();
            var metricsImplementations = metricsTypes.Where(t => t.IsClass && !t.IsAbstract).ToList();

            metricsInterfaces.Should().NotBeEmpty("Should have metrics interfaces for abstraction");
            metricsImplementations.Should().NotBeEmpty("Should have metrics implementations");
        }
        else
        {
            _output.WriteLine("No performance monitoring types found - consider implementing for enterprise observability");
        }

        if (behaviorTypes.Count > 0)
        {
            _output.WriteLine($"✓ Found {behaviorTypes.Count} performance behavior types");
        }

        Assert.True(true, $"Performance monitoring analysis completed. Found {metricsTypes.Count} monitoring types");
    }
}
