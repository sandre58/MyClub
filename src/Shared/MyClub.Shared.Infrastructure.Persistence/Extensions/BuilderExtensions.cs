// -----------------------------------------------------------------------
// <copyright file="BuilderExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.Shared.Infrastructure.Persistence.Conventions;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Kernel.Interfaces;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Humanizer;

namespace MyClub.Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for Entity Framework Core builders providing standardized configuration
/// patterns for domain entities, relationships, and value objects. This class centralizes
/// common configuration logic to ensure consistency across all entity configurations.
/// </summary>
/// <remarks>
/// The BuilderExtensions provides a comprehensive set of helper methods that encapsulate
/// Entity Framework Core configuration patterns specific to the football domain, including
/// strongly-typed ID handling, join entity configuration, value object ownership, and
/// complex relationship mapping. These extensions ensure consistent database schema
/// generation while reducing configuration boilerplate across entity configurations.
/// </remarks>
public static class BuilderExtensions
{
    #region Table

    /// <summary>
    /// Configures table name using pluralized entity type name with standardized naming convention.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for table name generation.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    /// <returns>The configured entity type builder.</returns>
    public static EntityTypeBuilder ToTable<TEntity>(this EntityTypeBuilder builder)
        where TEntity : class => builder.ToTable(typeof(TEntity).Name.Pluralize(culture: ConventionHelper.DatabaseCulture));

    /// <summary>
    /// Configures table name for owned entity types with optional prefix for disambiguation.
    /// </summary>
    /// <typeparam name="TEntity">The owned entity type for table name generation.</typeparam>
    /// <param name="builder">The owned navigation builder to configure.</param>
    /// <param name="prefix">Optional prefix for table name disambiguation.</param>
    /// <returns>The configured owned navigation builder.</returns>
    public static OwnedNavigationBuilder ToTable<TEntity>(this OwnedNavigationBuilder builder, string? prefix = null)
        where TEntity : class => builder.ToTable($"{prefix}{typeof(TEntity).Name.Pluralize(culture: ConventionHelper.DatabaseCulture)}");

    #endregion

    #region Id

    /// <summary>
    /// Configures strongly-typed entity identifier as primary key with appropriate value converter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type containing the strongly-typed identifier.</typeparam>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    /// <returns>The configured property builder for the identifier.</returns>
    public static PropertyBuilder<TId> HasStronglyTypedId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : Entity<TId>
        where TId : EntityId<TId>
    {
        builder.HasKey(static x => x.Id);
        return builder.Property(static m => m.Id).HasConversion(new StronglyTypedIdConverter<TId>()).ValueGeneratedNever();
    }

    /// <summary>
    /// Configures strongly-typed entity identifier for owned entities with appropriate value converter.
    /// </summary>
    /// <typeparam name="TOwner">The owner entity type.</typeparam>
    /// <typeparam name="TEntity">The owned entity type containing the strongly-typed identifier.</typeparam>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="builder">The owned navigation builder to configure.</param>
    /// <returns>The configured property builder for the identifier.</returns>
    public static PropertyBuilder<TId> HasStronglyTypedId<TOwner, TEntity, TId>(this OwnedNavigationBuilder<TOwner, TEntity> builder)
        where TOwner : class
        where TEntity : Entity<TId>
        where TId : EntityId<TId>
    {
        builder.HasKey(static x => x.Id);
        return builder.Property(static m => m.Id).HasConversion(new StronglyTypedIdConverter<TId>()).ValueGeneratedNever();
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Applies nullable strongly-typed identifier conversion to property.
    /// </summary>
    public static PropertyBuilder<TId?> HasNullableStronglyTypedIdConversion<TId>(this PropertyBuilder<TId?> builder)
        where TId : EntityId<TId> => builder.HasConversion(new NullableStronglyTypedIdConverter<TId>());

    /// <summary>
    /// Applies strongly-typed identifier conversion to property.
    /// </summary>
    public static PropertyBuilder<TId> HasStronglyTypedIdConversion<TId>(this PropertyBuilder<TId> builder)
        where TId : EntityId<TId> => builder.HasConversion(new StronglyTypedIdConverter<TId>());

    /// <summary>
    /// Applies list converter to collection property with customizable delimiter.
    /// </summary>
    public static PropertyBuilder<ICollection<T>> HasListConversion<T>(this PropertyBuilder<ICollection<T>> builder, char delimiter = ConventionHelper.DefaultListSeparator)
        => builder.HasConversion(new ListConverter<T>(delimiter: delimiter));

    /// <summary>
    /// Applies list converter to List property with customizable delimiter.
    /// </summary>
    public static PropertyBuilder<List<T>> HasListConversion<T>(this PropertyBuilder<List<T>> builder, char delimiter = ConventionHelper.DefaultListSeparator)
        => builder.HasConversion(new ListConverter<T>(delimiter: delimiter));

    /// <summary>
    /// Applies enum list converter to collection property with customizable delimiter.
    /// </summary>
    public static PropertyBuilder<ICollection<TEnum>> HasEnumListConversion<TEnum>(this PropertyBuilder<ICollection<TEnum>> builder, char delimiter = ConventionHelper.DefaultListSeparator)
        where TEnum : struct, Enum
        => builder.HasConversion(new EnumListConverter<TEnum>(delimiter));

    /// <summary>
    /// Applies enum list converter to List property with customizable delimiter.
    /// </summary>
    public static PropertyBuilder<List<TEnum>> HasEnumListConversion<TEnum>(this PropertyBuilder<List<TEnum>> builder, char delimiter = ConventionHelper.DefaultListSeparator)
        where TEnum : struct, Enum
        => builder.HasConversion(new EnumListConverter<TEnum>(delimiter));

    /// <summary>
    /// Applies enum list converter to read-only collection property with customizable delimiter.
    /// </summary>
    public static PropertyBuilder<IReadOnlyCollection<TEnum>> HasEnumListConversion<TEnum>(this PropertyBuilder<IReadOnlyCollection<TEnum>> builder, char delimiter = ConventionHelper.DefaultListSeparator)
        where TEnum : struct, Enum
        => builder.HasConversion(new EnumListConverter<TEnum>(delimiter));

    #endregion

    #region Configure

    /// <summary>
    /// Configures entity with standardized table name and strongly-typed identifier setup.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure.</typeparam>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    /// <param name="tableName">Optional custom table name override.</param>
    /// <returns>The configured entity type builder.</returns>
    public static EntityTypeBuilder<TEntity> ConfigureEntity<TEntity, TId>(this EntityTypeBuilder<TEntity> builder, string? tableName = null)
        where TEntity : Entity<TId>
        where TId : EntityId<TId>
    {
        if (!string.IsNullOrEmpty(tableName))
            builder.ToTable(tableName);
        else
            builder.ToTable<TEntity>();

        builder.HasStronglyTypedId<TEntity, TId>();

        return builder;
    }

    /// <summary>
    /// Configures join entity with standardized naming and relationship patterns.
    /// </summary>
    /// <typeparam name="TJoinEntity">The join entity type.</typeparam>
    /// <typeparam name="TPrincipalEntity">The principal entity type.</typeparam>
    /// <typeparam name="TForeignEntity">The foreign entity type.</typeparam>
    /// <typeparam name="TPrincipalId">The principal entity identifier type.</typeparam>
    /// <typeparam name="TForeignId">The foreign entity identifier type.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    /// <returns>The configured entity type builder.</returns>
    public static EntityTypeBuilder<TJoinEntity> ConfigureJoinEntity<TJoinEntity, TPrincipalEntity, TForeignEntity, TPrincipalId, TForeignId>(this EntityTypeBuilder<TJoinEntity> builder)
        where TJoinEntity : EntityLink<TPrincipalEntity, TForeignEntity, TPrincipalId, TForeignId>
        where TPrincipalEntity : Entity<TPrincipalId>
        where TForeignEntity : Entity<TForeignId>
        where TPrincipalId : EntityId<TPrincipalId>
        where TForeignId : EntityId<TForeignId>
    {
        const string idSuffix = "Id";
        var principalEntityName = typeof(TPrincipalEntity).Name;
        var principalEntityIdColumnName = $"{principalEntityName}{idSuffix}";
        var foreignEntityName = typeof(TForeignEntity).Name;
        var foreignEntityIdColumnName = $"{foreignEntityName}{idSuffix}";

        builder.ToTable<TJoinEntity>();

        builder.HasKey(static x => new { x.LeftId, x.RightId });
        builder.Property(static x => x.LeftId).HasStronglyTypedIdConversion().HasColumnName(principalEntityIdColumnName);
        builder.Property(static x => x.RightId).HasStronglyTypedIdConversion().HasColumnName(foreignEntityIdColumnName);

        builder.HasOne(static x => x.Right)
            .WithMany()
            .HasPrincipalKey(static x => x.Id)
            .HasForeignKey(static x => x.RightId)
            .IsRequired();

        builder.HasOne(static x => x.Left)
            .WithMany()
            .HasPrincipalKey(static x => x.Id)
            .HasForeignKey(static x => x.LeftId)
            .IsRequired();

        builder.HasIndex(static x => x.LeftId);
        builder.HasIndex(static x => x.RightId);

        return builder;
    }

    #endregion

    #region HasMany

    /// <summary>
    /// Configures many-to-many relationship through explicit join entity.
    /// </summary>
    public static EntityTypeBuilder<TPrincipalEntity> HasMany<TJoinEntity, TPrincipalEntity, TForeignEntity, TPrincipalId, TForeignId>(this EntityTypeBuilder<TPrincipalEntity> builder, Action<EntityTypeBuilder<TPrincipalEntity>>? builderAction = null, string? propertyName = null)
        where TJoinEntity : EntityLink<TPrincipalEntity, TForeignEntity, TPrincipalId, TForeignId>
        where TPrincipalEntity : Entity<TPrincipalId>
        where TForeignEntity : Entity<TForeignId>
        where TPrincipalId : EntityId<TPrincipalId>
        where TForeignId : EntityId<TForeignId>
    {
        var finalPropertyName = propertyName ?? typeof(TForeignEntity).Name.Pluralize(culture: ConventionHelper.DatabaseCulture);

        builder.Ignore(finalPropertyName);

        builder.HasMany<TJoinEntity>()
            .WithOne()
            .HasForeignKey(static x => x.LeftId)
            .OnDelete(DeleteBehavior.Cascade);

        builderAction?.Invoke(builder);

        return builder;
    }

    #endregion

    #region Owns

    /// <summary>
    /// Configures owned entity collection with complex relationship and foreign key setup.
    /// </summary>
    public static OwnedNavigationBuilder<TOwner, TEntity> OwnsMany<TOwner, TEntity, T, TId, TOwnerId>(this OwnedNavigationBuilder<TOwner, TEntity> builder,
        Expression<Func<TEntity, object?>> principalKeyExpression,
        Expression<Func<T, object?>> foreignKeyExpression,
        Action<OwnedNavigationBuilder<TEntity, T>>? buildAction = null,
        string? ownedPropertyName = null,
        string? prefixTable = null)
        where TOwner : class
        where TEntity : class
        where T : Entity<TId>
        where TId : EntityId<TId>
        where TOwnerId : EntityId<TOwnerId>
    {
        var propertyName = ownedPropertyName ?? typeof(T).Name.Pluralize(culture: ConventionHelper.DatabaseCulture);
        builder.OwnsMany<T>(propertyName.ToPrivateFieldName(), y =>
        {
            y.ToTable<T>(prefixTable);

            y.HasStronglyTypedId<TEntity, T, TId>();

            y.WithOwner().HasPrincipalKey(principalKeyExpression).HasForeignKey(foreignKeyExpression);
            y.Property(foreignKeyExpression).HasConversion(new ValueConverter<TOwnerId, Guid>(static z => z.Value, static z => EntityId.From<TOwnerId>(z)));

            y.HasIndex(foreignKeyExpression);

            buildAction?.Invoke(y);
        });

        builder.Ignore(propertyName);
        builder.Navigation(propertyName.ToPrivateFieldName()).UsePropertyAccessMode(PropertyAccessMode.Field);

        return builder;
    }

    #endregion

    /// <summary>
    /// Configures auditable properties for entities implementing IAuditable interface.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsAuditableProperties<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(static x => x.CreatedAt);
        builder.Property(static x => x.CreatedBy);
        builder.Property(static x => x.ModifiedAt);
        builder.Property(static x => x.ModifiedBy);

        return builder;
    }

    /// <summary>
    /// Extracts member name from lambda expression for property identification.
    /// </summary>
    public static string GetMemberName<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression)
        where TEntity : class
        => expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression memberOperand } => memberOperand.Member.Name,
            _ => string.Empty
        };

    /// <summary>
    /// Converts property name to private field naming convention for EF Core field access.
    /// </summary>
    public static string ToPrivateFieldName(this string propertyName) => "_" + char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
