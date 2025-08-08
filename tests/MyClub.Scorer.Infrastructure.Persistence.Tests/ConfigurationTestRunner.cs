// -----------------------------------------------------------------------
// <copyright file="ConfigurationTestRunner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class ConfigurationTestRunner(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void RunAllConfigurationValidations()
    {
        Output.WriteLine("=== STARTING COMPLETE EF CORE CONFIGURATION VALIDATION ===\n");

        var results = new List<(string TestName, bool Success, string Message)>
        {
            // Test 1: Model Creation
            TestModelCreation(), // Test 2: All Entities Have Tables
            TestAllEntitiesHaveTables(), // Test 3: All Entities Have Primary Keys
            TestAllEntitiesHavePrimaryKeys(), // Test 4: All Required Properties Are Configured
            TestRequiredPropertiesConfiguration(), // Test 5: All Value Converters Work
            TestValueConverters(), // Test 6: All Foreign Keys Are Valid
            TestForeignKeys(), // Test 7: All Owned Entities Are Valid
            TestOwnedEntities(), // Test 8: Database Can Be Created
            TestDatabaseCreation()
        };

        // Summary
        PrintSummary(results);

        // Assert all tests passed
        var failedTests = results.Where(static r => !r.Success).ToList();
        if (failedTests.Count != 0)
        {
            var failureMessages = string.Join("\n", failedTests.Select(static t => $"❌ {t.TestName}: {t.Message}"));
            Assert.Fail($"Configuration validation failed:\n{failureMessages}");
        }

        Output.WriteLine("🎉 ALL EF CORE CONFIGURATION TESTS PASSED!");
    }

    private (string TestName, bool Success, string Message) TestModelCreation()
    {
        try
        {
            var entityTypes = Context.Model.GetEntityTypes().ToList();
            var message = $"Model created successfully with {entityTypes.Count} entity types";
            Output.WriteLine($"✅ Model Creation: {message}");
            return ("Model Creation", true, message);
        }
        catch (Exception ex)
        {
            var message = $"Failed to create model: {ex.Message}";
            Output.WriteLine($"❌ Model Creation: {message}");
            return ("Model Creation", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestAllEntitiesHaveTables()
    {
        try
        {
            var entityTypes = Context.Model.GetEntityTypes()
                .Where(static e => !e.IsOwned())
                .ToList();

            var entitiesWithoutTables = (from entityType in entityTypes let tableName = entityType.GetTableName() where string.IsNullOrWhiteSpace(tableName) select entityType.ClrType.Name).ToList();

            if (entitiesWithoutTables.Count != 0)
            {
                var message = $"Entities without table names: {string.Join(", ", entitiesWithoutTables)}";
                Output.WriteLine($"❌ Table Mapping: {message}");
                return ("Table Mapping", false, message);
            }

            var message1 = $"All {entityTypes.Count} non-owned entities have valid table mappings";
            Output.WriteLine($"✅ Table Mapping: {message1}");
            return ("Table Mapping", true, message1);
        }
        catch (Exception ex)
        {
            var message = $"Error checking table mappings: {ex.Message}";
            Output.WriteLine($"❌ Table Mapping: {message}");
            return ("Table Mapping", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestAllEntitiesHavePrimaryKeys()
    {
        try
        {
            var entityTypes = Context.Model.GetEntityTypes()
                .Where(static e => !e.IsOwned())
                .ToList();

            var entitiesWithoutPks = (from entityType in entityTypes let primaryKey = entityType.FindPrimaryKey() where primaryKey == null || !primaryKey.Properties.Any() select entityType.ClrType.Name).ToList();

            if (entitiesWithoutPks.Count != 0)
            {
                var message = $"Entities without primary keys: {string.Join(", ", entitiesWithoutPks)}";
                Output.WriteLine($"❌ Primary Keys: {message}");
                return ("Primary Keys", false, message);
            }

            var message1 = $"All {entityTypes.Count} non-owned entities have valid primary keys";
            Output.WriteLine($"✅ Primary Keys: {message1}");
            return ("Primary Keys", true, message1);
        }
        catch (Exception ex)
        {
            var message = $"Error checking primary keys: {ex.Message}";
            Output.WriteLine($"❌ Primary Keys: {message}");
            return ("Primary Keys", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestRequiredPropertiesConfiguration()
    {
        try
        {
            var issues = new List<string>();
            var entityTypes = Context.Model.GetEntityTypes().ToList();

            foreach (var entityType in entityTypes)
            {
                issues.AddRange(from property in entityType.GetProperties() where property.Name == "Id" && property.IsNullable select $"{entityType.ClrType.Name}.{property.Name} should not be nullable");
            }

            if (issues.Count != 0)
            {
                var message = $"Property configuration issues found: {string.Join("; ", issues)}";
                Output.WriteLine($"⚠️ Required Properties: {message}");
                return ("Required Properties", false, message);
            }

            const string message1 = "All required properties are correctly configured";
            Output.WriteLine($"✅ Required Properties: {message1}");
            return ("Required Properties", true, message1);
        }
        catch (Exception ex)
        {
            var message = $"Error checking required properties: {ex.Message}";
            Output.WriteLine($"❌ Required Properties: {message}");
            return ("Required Properties", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestValueConverters()
    {
        try
        {
            var converterCount = 0;
            var entityTypes = Context.Model.GetEntityTypes().ToList();

            foreach (var property in entityTypes.SelectMany(static entityType => entityType.GetProperties()))
            {
                if (property.GetValueConverter() != null)
                {
                    converterCount++;
                }
            }

            var message = $"Found {converterCount} properties with value converters";
            Output.WriteLine($"✅ Value Converters: {message}");
            return ("Value Converters", true, message);
        }
        catch (Exception ex)
        {
            var message = $"Error checking value converters: {ex.Message}";
            Output.WriteLine($"❌ Value Converters: {message}");
            return ("Value Converters", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestForeignKeys()
    {
        try
        {
            var entityTypes = Context.Model.GetEntityTypes().ToList();

            var foreignKeyCount = entityTypes.Sum(static entityType => entityType.GetForeignKeys().Count());

            var message = $"Found {foreignKeyCount} valid foreign key relationships";
            Output.WriteLine($"✅ Foreign Keys: {message}");
            return ("Foreign Keys", true, message);
        }
        catch (Exception ex)
        {
            var message = $"Error checking foreign keys: {ex.Message}";
            Output.WriteLine($"❌ Foreign Keys: {message}");
            return ("Foreign Keys", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestOwnedEntities()
    {
        try
        {
            var ownedEntityTypes = Context.Model.GetEntityTypes()
                .Where(static e => e.IsOwned())
                .ToList();

            var orphanedOwned = (from ownedEntity in ownedEntityTypes let ownership = ownedEntity.FindOwnership() where ownership == null select ownedEntity.ClrType.Name).ToList();

            if (orphanedOwned.Count != 0)
            {
                var message = $"Orphaned owned entities: {string.Join(", ", orphanedOwned)}";
                Output.WriteLine($"❌ Owned Entities: {message}");
                return ("Owned Entities", false, message);
            }

            var message1 = $"All {ownedEntityTypes.Count} owned entities are correctly configured";
            Output.WriteLine($"✅ Owned Entities: {message1}");
            return ("Owned Entities", true, message1);
        }
        catch (Exception ex)
        {
            var message = $"Error checking owned entities: {ex.Message}";
            Output.WriteLine($"❌ Owned Entities: {message}");
            return ("Owned Entities", false, message);
        }
    }

    private (string TestName, bool Success, string Message) TestDatabaseCreation()
    {
        try
        {
            var created = Context.Database.EnsureCreated();
            var message = created ? "Database created successfully" : "Database already exists";
            Output.WriteLine($"✅ Database Creation: {message}");
            return ("Database Creation", true, message);
        }
        catch (Exception ex)
        {
            var message = $"Failed to create database: {ex.Message}";
            Output.WriteLine($"❌ Database Creation: {message}");
            return ("Database Creation", false, message);
        }
    }

    private void PrintSummary(List<(string TestName, bool Success, string Message)> results)
    {
        Output.WriteLine("\n=== CONFIGURATION VALIDATION SUMMARY ===");

        var passed = results.Count(static r => r.Success);
        var total = results.Count;

        Output.WriteLine($"Tests Passed: {passed}/{total}");
        Output.WriteLine($"Success Rate: {passed * 100.0 / total:F1}%\n");

        foreach (var (testName, success, message) in results)
        {
            var icon = success ? "✅" : "❌";
            Output.WriteLine($"{icon} {testName}: {message}");
        }

        Output.WriteLine("\n=== END SUMMARY ===\n");
    }
}
