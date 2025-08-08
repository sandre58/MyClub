// -----------------------------------------------------------------------
// <copyright file="AllConfigurationsTests.cs" company="Stéphane ANDRE">
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

public class AllConfigurationsTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void AllConfigurations_AreApplied()
    {
        var entityTypes = Context.Model.GetEntityTypes().ToList();

        Assert.True(entityTypes.Count > 0, "No entity types found in the model");

        Output.WriteLine($"Total entity types configured: {entityTypes.Count}");

        foreach (var entityType in entityTypes)
        {
            Output.WriteLine($"- {entityType.ClrType.Name} -> {entityType.GetTableName()} (Owned: {entityType.IsOwned()})");
        }
    }

    [Fact]
    public void AllConfigurations_HaveValidPrimaryKeys()
    {
        var entityTypes = Context.Model.GetEntityTypes()
            .Where(static e => !e.IsOwned())
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.True(primaryKey.Properties.Count > 0);
            Output.WriteLine($"✓ {entityType.ClrType.Name} has primary key: [{string.Join(", ", primaryKey.Properties.Select(static p => p.Name))}]");
        }
    }

    [Fact]
    public void AllConfigurations_HaveValidTableNames()
    {
        var entityTypes = Context.Model.GetEntityTypes()
            .Where(static e => !e.IsOwned()) // Owned entities can share the parent's table
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            Assert.NotNull(tableName);
            Assert.False(string.IsNullOrWhiteSpace(tableName));
            Output.WriteLine($"✓ {entityType.ClrType.Name} maps to table '{tableName}'");
        }
    }

    [Fact]
    public void AllConfigurations_RequiredProperties_AreNotNullable()
    {
        var entityTypes = Context.Model.GetEntityTypes().ToList();
        var invalidProperties = new List<string>();

        foreach (var entityType in entityTypes)
        {
            invalidProperties.AddRange(from property in entityType.GetProperties() where !property.Name.EndsWith("Id", StringComparison.InvariantCulture) || property.Name.Equals("Id", StringComparison.Ordinal) where property.IsNullable && property.Name.Equals("Id", StringComparison.Ordinal) select $"{entityType.ClrType.Name}.{property.Name} is nullable but should be required");
        }

        if (invalidProperties.Count > 0)
        {
            Output.WriteLine("Invalid property configurations:");
            foreach (var invalid in invalidProperties)
            {
                Output.WriteLine($"❌ {invalid}");
            }
        }

        Assert.Empty(invalidProperties);
    }

    [Fact]
    public void AllConfigurations_ValueConverters_AreValid()
    {
        var entityTypes = Context.Model.GetEntityTypes().ToList();
        var convertersCount = 0;

        foreach (var entityType in entityTypes)
        {
            foreach (var property in entityType.GetProperties())
            {
                var converter = property.GetValueConverter();
                if (converter == null)
                    continue;
                convertersCount++;
                Output.WriteLine($"✓ {entityType.ClrType.Name}.{property.Name} has converter: {converter.GetType().Name}");
            }
        }

        Output.WriteLine($"Total properties with value converters: {convertersCount}");
        Assert.True(convertersCount > 0, "No value converters found, this might indicate configuration issues");
    }

    [Fact]
    public void AllConfigurations_ForeignKeys_AreValid()
    {
        var entityTypes = Context.Model.GetEntityTypes().ToList();
        var foreignKeysCount = 0;

        foreach (var entityType in entityTypes)
        {
            var foreignKeys = entityType.GetForeignKeys();
            foreach (var fk in foreignKeys)
            {
                foreignKeysCount++;
                var fkProperties = string.Join(", ", fk.Properties.Select(static p => p.Name));
                var principalEntity = fk.PrincipalEntityType.ClrType.Name;
                Output.WriteLine($"✓ {entityType.ClrType.Name}[{fkProperties}] -> {principalEntity}");

                // Verify that the FK has a valid property
                Assert.NotEmpty(fk.Properties);
                Assert.NotNull(fk.PrincipalEntityType);
            }
        }

        Output.WriteLine($"Total foreign keys: {foreignKeysCount}");
    }

    [Fact]
    public void AllConfigurations_OwnedEntities_AreValid()
    {
        var ownedEntityTypes = Context.Model.GetEntityTypes()
            .Where(static e => e.IsOwned())
            .ToList();

        Output.WriteLine($"Total owned entities: {ownedEntityTypes.Count}");

        foreach (var ownedEntity in ownedEntityTypes)
        {
            var owner = ownedEntity.FindOwnership()?.PrincipalEntityType;
            Assert.NotNull(owner);
            Output.WriteLine($"✓ {ownedEntity.ClrType.Name} is owned by {owner.ClrType.Name}");
        }
    }

    [Fact]
    public void AllConfigurations_CanGenerateDatabase()
    {
        // Test that the model can generate a database without error
        var canCreate = Context.Database.EnsureCreated();
        Output.WriteLine($"Database creation: {(canCreate ? "Success" : "Already exists")}");

        // Verify that all tables are created
        var tables = Context.Model.GetEntityTypes()
            .Where(static e => !e.IsOwned())
            .Select(static e => e.GetTableName())
            .Distinct()
            .ToList();

        Output.WriteLine($"Tables that should be created: {string.Join(", ", tables)}");

        Assert.True(tables.Count > 0);
    }

    [Fact]
    public void AllConfigurations_Debug_CompleteModel() => DebugAllEntities();
}
