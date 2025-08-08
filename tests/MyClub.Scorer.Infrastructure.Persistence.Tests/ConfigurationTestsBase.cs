// -----------------------------------------------------------------------
// <copyright file="ConfigurationTestsBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Base class for test configurations")]
public abstract class ConfigurationTestsBase(ITestOutputHelper output) : IDisposable
{
    protected ITestOutputHelper Output { get; } = output;

    protected ScorerDbContext Context { get; } = CreateTestContext();

    private static ScorerDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ScorerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new ScorerDbContext(options);

        // Force model creation
        _ = context.Model;

        return context;
    }

    protected IEntityType GetEntityType<T>()
        where T : class
    {
        var entityType = Context.Model.FindEntityType(typeof(T));
        Assert.NotNull(entityType);
        return entityType;
    }

    protected void AssertPropertyExists<T>(string propertyName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Output.WriteLine($"✓ Property '{propertyName}' exists for {typeof(T).Name}");
    }

    protected void AssertPropertyType<T>(string propertyName, Type expectedType)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(expectedType, property.ClrType);
        Output.WriteLine($"✓ Property '{propertyName}' has correct type {expectedType.Name} for {typeof(T).Name}");
    }

    protected void AssertColumnName<T>(string propertyName, string expectedColumnName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(expectedColumnName, property.GetColumnName());
        Output.WriteLine($"✓ Property '{propertyName}' maps to column '{expectedColumnName}' for {typeof(T).Name}");
    }

    protected void AssertMaxLength<T>(string propertyName, int expectedMaxLength)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(expectedMaxLength, property.GetMaxLength());
        Output.WriteLine($"✓ Property '{propertyName}' has max length {expectedMaxLength} for {typeof(T).Name}");
    }

    protected void AssertIsRequired<T>(string propertyName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.False(property.IsNullable);
        Output.WriteLine($"✓ Property '{propertyName}' is required for {typeof(T).Name}");
    }

    protected void AssertIsOptional<T>(string propertyName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.True(property.IsNullable);
        Output.WriteLine($"✓ Property '{propertyName}' is optional for {typeof(T).Name}");
    }

    protected void AssertTableName<T>(string expectedTableName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        Assert.Equal(expectedTableName, entityType.GetTableName());
        Output.WriteLine($"✓ Entity {typeof(T).Name} maps to table '{expectedTableName}'");
    }

    protected void AssertPrimaryKey<T>(params string[] keyPropertyNames)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);

        var actualKeyNames = primaryKey.Properties.Select(static p => p.Name).ToArray();
        Assert.Equal(keyPropertyNames.OrderBy(static x => x), actualKeyNames.OrderBy(static x => x));
        Output.WriteLine($"✓ Entity {typeof(T).Name} has primary key: [{string.Join(", ", keyPropertyNames)}]");
    }

    protected void AssertForeignKey<T>(string foreignKeyProperty, string principalEntityType)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var foreignKeys = entityType.GetForeignKeys();

        var fk = foreignKeys.FirstOrDefault(fk =>
            fk.Properties.Any(p => p.Name == foreignKeyProperty));

        Assert.NotNull(fk);
        Assert.Equal(principalEntityType, fk.PrincipalEntityType.ClrType.Name);
        Output.WriteLine($"✓ Entity {typeof(T).Name} has foreign key '{foreignKeyProperty}' to {principalEntityType}");
    }

    protected void AssertOwnedEntity<T>(string ownedPropertyName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var navigation = entityType.FindNavigation(ownedPropertyName);
        Assert.NotNull(navigation);
        Assert.True(navigation.TargetEntityType.IsOwned());
        Output.WriteLine($"✓ Entity {typeof(T).Name} owns '{ownedPropertyName}'");
    }

    protected void AssertHasConversion<T>(string propertyName)
        where T : class
    {
        var entityType = GetEntityType<T>();
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.NotNull(property.GetValueConverter());
        Output.WriteLine($"✓ Property '{propertyName}' has value converter for {typeof(T).Name}");
    }

    protected void DebugAllProperties<T>()
        where T : class
    {
        var entityType = GetEntityType<T>();
        Output.WriteLine($"\n--- All properties for {typeof(T).Name} ---");
        foreach (var property in entityType.GetProperties())
        {
            Output.WriteLine($"Property: {property.Name} | Type: {property.ClrType.Name} | Column: {property.GetColumnName()} | Required: {!property.IsNullable}");
        }

        Output.WriteLine("--- End properties ---\n");
    }

    protected void DebugAllEntities()
    {
        Output.WriteLine("\n--- All configured entities ---");
        foreach (var entityType in Context.Model.GetEntityTypes())
        {
            Output.WriteLine($"Entity: {entityType.ClrType.Name} | Table: {entityType.GetTableName()} | Owned: {entityType.IsOwned()}");
        }

        Output.WriteLine("--- End entities ---\n");
    }

    #region IDisposable

    private bool _isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            // free managed resources
            Context.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
