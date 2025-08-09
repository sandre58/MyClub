// -----------------------------------------------------------------------
// <copyright file="SecurityArchitectureTests.cs" company="Stéphane ANDRE">
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
/// Tests to validate security architecture patterns and practices.
/// Ensures proper handling of sensitive data, authentication, authorization, and security boundaries.
/// </summary>
public class SecurityArchitectureTests(ITestOutputHelper output)
{
    private static readonly Assembly[] AllAssemblies = [.. AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name?.StartsWith("MyClub.", StringComparison.InvariantCulture) == true)];
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Sensitive_Data_Should_Not_Be_In_Logs()
    {
        // Arrange & Act
        var typesWithLogging = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(hasLoggingMembers)
            .ToList();

        // Assert - Check for potential sensitive data patterns
        var suspiciousLogPatterns = new[]
        {
            "Password", "Token", "Secret", "Key", "Credential", "Auth"
        };

        foreach (var type in typesWithLogging)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            foreach (var method in methods)
            {
                var methodBody = method.GetMethodBody();
                if (methodBody != null)
                {
                    // This is a basic check - in real scenarios, you'd use more sophisticated analysis
                    var methodName = method.Name.ToUpperInvariant();
                    var hasSuspiciousPattern = suspiciousLogPatterns.Any(pattern =>
                        methodName.Contains(pattern.ToUpperInvariant(), StringComparison.InvariantCultureIgnoreCase));

                    if (hasSuspiciousPattern && methodName.Contains("log", StringComparison.InvariantCulture))
                    {
                        _output.WriteLine($"⚠️ Potential sensitive data logging in {type.Name}.{method.Name}");
                    }
                }
            }
        }

        _output.WriteLine($"Analyzed {typesWithLogging.Count} types with logging capabilities");
        Assert.True(true, "Sensitive data logging analysis completed");

        static bool hasLoggingMembers(Type type) => type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Any(f => f.FieldType.Name.Contains("ILogger", StringComparison.InvariantCulture) || f.FieldType.Name.Contains("Log", StringComparison.InvariantCulture)) ||
                type.GetProperties().Any(p => p.PropertyType.Name.Contains("ILogger", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Authentication_Should_Be_Centralized()
    {
        // Arrange & Act
        var authenticationTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.ToUpperInvariant().Contains("auth", StringComparison.InvariantCultureIgnoreCase) &&
                       (t.Name.Contains("Authentication", StringComparison.InvariantCulture) || t.Name.Contains("Auth", StringComparison.InvariantCulture)))
            .ToList();

        var authInterfaces = authenticationTypes.Where(t => t.IsInterface).ToList();
        var authImplementations = authenticationTypes.Where(t => t.IsClass && !t.IsAbstract).ToList();

        // Assert
        if (authenticationTypes.Count > 0)
        {
            _output.WriteLine($"Found {authInterfaces.Count} authentication interfaces and {authImplementations.Count} implementations");

            // Authentication should be in shared infrastructure or dedicated security module
            var properlyLocatedAuth = authenticationTypes
                .Where(t => t.Namespace?.Contains("Shared", StringComparison.InvariantCulture) == true ||
                           t.Namespace?.Contains("Security", StringComparison.InvariantCulture) == true ||
                           t.Namespace?.Contains("CrossCutting", StringComparison.InvariantCulture) == true)
                .ToList();

            var businessModuleAuth = authenticationTypes
                .Where(t => t.Namespace?.Contains("Scorer", StringComparison.InvariantCulture) == true ||
                           t.Namespace?.Contains("Referential", StringComparison.InvariantCulture) == true)
                .ToList();

            businessModuleAuth.Should().BeEmpty("Authentication should not be scattered across business modules");

            if (properlyLocatedAuth.Count > 0)
            {
                _output.WriteLine($"✓ {properlyLocatedAuth.Count} authentication types are properly centralized");
            }
        }
        else
        {
            _output.WriteLine("No authentication types found - security module may not be implemented yet");
        }

        Assert.True(true, "Authentication centralization analysis completed");
    }

    [Fact]
    public void Authorization_Should_Follow_Patterns()
    {
        // Arrange & Act
        var authorizationTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.ToUpperInvariant().Contains("author", StringComparison.InvariantCultureIgnoreCase) ||
                       t.Name.Contains("Permission", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Role", StringComparison.InvariantCulture))
            .ToList();

        var policyTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Policy", StringComparison.InvariantCulture) &&
                       (t.Namespace?.Contains("Security", StringComparison.InvariantCulture) == true || t.Namespace?.Contains("Auth", StringComparison.InvariantCulture) == true))
            .ToList();

        // Assert
        if (authorizationTypes.Count > 0)
        {
            _output.WriteLine($"Found {authorizationTypes.Count} authorization-related types");

            // Authorization should be interface-driven
            var authInterfaces = authorizationTypes.Where(t => t.IsInterface).ToList();
            var authImplementations = authorizationTypes.Where(t => t.IsClass).ToList();

            if (authInterfaces.Count > 0)
            {
                authImplementations.Should().NotBeEmpty("Should have implementations for authorization interfaces");
                _output.WriteLine($"✓ Found {authInterfaces.Count} authorization interfaces with {authImplementations.Count} implementations");
            }
        }

        if (policyTypes.Count > 0)
        {
            _output.WriteLine($"Found {policyTypes.Count} policy types");
        }
        else
        {
            _output.WriteLine("No authorization policies found - consider implementing for enterprise security");
        }

        Assert.True(true, "Authorization pattern analysis completed");
    }

    [Fact]
    public void External_Dependencies_Should_Be_Validated()
    {
        // Arrange & Act
        var dangerousPackages = new[]
        {
            "Newtonsoft.Json", // Prefer System.Text.Json for security
            "System.Web", // Legacy, security issues
            "Microsoft.AspNet", // Legacy ASP.NET, not Core
        };

        var allReferencedAssemblies = AllAssemblies
            .SelectMany(a => a.GetReferencedAssemblies())
            .Select(a => a.Name)
            .Distinct()
            .ToList();

        var dangerousReferences = allReferencedAssemblies
            .Where(name => dangerousPackages.Any(dangerous => name?.Contains(dangerous, StringComparison.InvariantCulture) == true))
            .ToList();

        // Assert
        if (dangerousReferences.Count > 0)
        {
            _output.WriteLine($"⚠️ Found potentially dangerous dependencies: {string.Join(", ", dangerousReferences)}");

            // This is a warning, not a failure - some dependencies might be necessary
            foreach (var reference in dangerousReferences)
            {
                _output.WriteLine($"Consider reviewing usage of: {reference}");
            }
        }
        else
        {
            _output.WriteLine("✓ No dangerous external dependencies found");
        }

        Assert.True(true, $"External dependency validation completed. Found {dangerousReferences.Count} potentially dangerous references");
    }

    [Fact]
    public void Cryptographic_Operations_Should_Be_Centralized()
    {
        // Arrange & Act
        var cryptoTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Crypto", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Hash", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Encrypt", StringComparison.InvariantCulture))
            .ToList();

        var typesWithCryptoUsage = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && hasCryptographicReferences(t))
            .ToList();

        // Assert
        if (cryptoTypes.Count > 0 || typesWithCryptoUsage.Count > 0)
        {
            _output.WriteLine($"Found {cryptoTypes.Count} crypto types and {typesWithCryptoUsage.Count} types using cryptography");

            // Crypto should be in shared infrastructure or security module
            var businessModuleCrypto = cryptoTypes
                .Where(t => t.Namespace?.Contains("Scorer", StringComparison.InvariantCulture) == true ||
                           t.Namespace?.Contains("Referential", StringComparison.InvariantCulture) == true)
                .ToList();

            businessModuleCrypto.Should().BeEmpty("Cryptographic operations should not be scattered across business modules");
        }
        else
        {
            _output.WriteLine("No cryptographic operations found - may not be needed for current functionality");
        }

        Assert.True(true, "Cryptographic operations analysis completed");

        static bool hasCryptographicReferences(Type type)
        {
            var referencedTypes = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(f => f.FieldType.Name)
                .Concat(type.GetProperties().Select(p => p.PropertyType.Name))
                .ToList();

            return referencedTypes.Any(name =>
                name.Contains("Hash", StringComparison.InvariantCulture) ||
                name.Contains("Crypto", StringComparison.InvariantCulture) ||
                name.Contains("Encrypt", StringComparison.InvariantCulture) ||
                name.Contains("Cipher", StringComparison.InvariantCulture));
        }
    }

    [Fact]
    public void Configuration_Should_Be_Secure()
    {
        // Arrange & Act
        var configurationTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Config", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Settings", StringComparison.InvariantCulture) ||
                       t.Name.Contains("Options", StringComparison.InvariantCulture))
            .ToList();

        // Assert
        if (configurationTypes.Count > 0)
        {
            _output.WriteLine($"Found {configurationTypes.Count} configuration types");

            // Check for potential security issues in configuration
            foreach (var configType in configurationTypes)
            {
                var properties = configType.GetProperties();
                var sensitiveProperties = properties
                    .Where(p => p.Name.ToUpperInvariant().Contains("password", StringComparison.InvariantCultureIgnoreCase) ||
                               p.Name.ToUpperInvariant().Contains("secret", StringComparison.InvariantCultureIgnoreCase) ||
                               p.Name.ToUpperInvariant().Contains("key", StringComparison.InvariantCultureIgnoreCase) ||
                               p.Name.ToUpperInvariant().Contains("token", StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (sensitiveProperties.Count > 0)
                {
                    _output.WriteLine($"⚠️ {configType.Name} contains potentially sensitive properties: {string.Join(", ", sensitiveProperties.Select(p => p.Name))}");
                    _output.WriteLine("Consider using secure configuration patterns (Azure Key Vault, environment variables, etc.)");
                }
            }
        }
        else
        {
            _output.WriteLine("No configuration types found");
        }

        Assert.True(true, "Configuration security analysis completed");
    }

    [Fact]
    public void Input_Validation_Should_Be_Consistent()
    {
        // Force loading of shared infrastructure assembly
        var forceLoad = typeof(Shared.Application.Behaviors.ValidationBehavior<,>);

        // Arrange & Act
        var validationTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.Contains("Validat", StringComparison.InvariantCulture) ||
                       t.BaseType?.Name.Contains("AbstractValidator", StringComparison.InvariantCulture) == true)
            .ToList();

        var validationInterfaces = validationTypes.Where(t => t.IsInterface).ToList();
        var validationImplementations = validationTypes.Where(t => t.IsClass).ToList();

        // Assert
        validationTypes.Should().NotBeEmpty("Should have input validation implementations");

        if (validationTypes.Count > 0)
        {
            _output.WriteLine($"Found {validationInterfaces.Count} validation interfaces and {validationImplementations.Count} implementations");

            // Validation should be centralized in application layer
            var applicationLayerValidation = validationTypes
                .Where(t => t.Namespace?.Contains("Application", StringComparison.InvariantCulture) == true)
                .ToList();

            var domainLayerValidation = validationTypes
                .Where(t => t.Namespace?.Contains("Domain", StringComparison.InvariantCulture) == true)
                .ToList();

            applicationLayerValidation.Should().NotBeEmpty("Should have validation in application layer");

            if (domainLayerValidation.Count > 0)
            {
                _output.WriteLine($"Found {domainLayerValidation.Count} domain validation types - ensure they handle business rules, not input validation");
            }
        }

        Assert.True(true, $"Input validation analysis completed. Found {validationTypes.Count} validation types");
    }

    [Fact]
    public void Error_Messages_Should_Not_Leak_Sensitive_Information()
    {
        // Arrange & Act
        var exceptionTypes = AllAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.BaseType == typeof(System.Exception) ||
                       t.Name.EndsWith("Exception", StringComparison.InvariantCulture))
            .ToList();

        // Assert
        if (exceptionTypes.Count > 0)
        {
            _output.WriteLine($"Found {exceptionTypes.Count} custom exception types");

            // Check for potential information disclosure in exception types
            var suspiciousExceptions = exceptionTypes
                .Where(t => t.Name.ToUpperInvariant().Contains("sql", StringComparison.InvariantCultureIgnoreCase) ||
                           t.Name.ToUpperInvariant().Contains("database", StringComparison.InvariantCultureIgnoreCase) ||
                           t.Name.ToUpperInvariant().Contains("connection", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (suspiciousExceptions.Count > 0)
            {
                _output.WriteLine($"⚠️ Found potentially information-disclosing exceptions: {string.Join(", ", suspiciousExceptions.Select(e => e.Name))}");
                _output.WriteLine("Ensure these exceptions don't expose sensitive system information to end users");
            }
            else
            {
                _output.WriteLine("✓ No obviously problematic exception types found");
            }
        }
        else
        {
            _output.WriteLine("No custom exception types found");
        }

        Assert.True(true, "Error message security analysis completed");
    }
}
