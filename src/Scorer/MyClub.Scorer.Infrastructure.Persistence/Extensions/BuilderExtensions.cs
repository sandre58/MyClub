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
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Converters;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Infrastructure.Persistence.Conventions;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Humanizer;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Infrastructure.Persistence.Extensions;

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
internal static class BuilderExtensions
{
    #region Configure

    /// <summary>
    /// Configures join entity specifically for match relationships.
    /// </summary>
    public static EntityTypeBuilder<TJoinEntity> ConfigureJoinMatch<TJoinEntity, TPrincipalEntity, TPrincipalId>(this EntityTypeBuilder<TJoinEntity> builder)
        where TJoinEntity : EntityMatch<TPrincipalEntity, TPrincipalId>
        where TPrincipalEntity : Entity<TPrincipalId>
        where TPrincipalId : EntityId<TPrincipalId>
        => builder.ConfigureJoinEntity<TJoinEntity, TPrincipalEntity, Match, TPrincipalId, MatchId>();

    /// <summary>
    /// Configures join entity specifically for team reference relationships.
    /// </summary>
    public static EntityTypeBuilder<TJoinEntity> ConfigureJoinTeam<TJoinEntity, TPrincipalEntity, TPrincipalId>(this EntityTypeBuilder<TJoinEntity> builder)
        where TJoinEntity : EntityTeam<TPrincipalId>
        where TPrincipalEntity : Entity<TPrincipalId>
        where TPrincipalId : EntityId<TPrincipalId>
    {
        builder.Property(static x => x.LeftId).HasStronglyTypedIdConversion().HasColumnName($"{typeof(TPrincipalEntity).Name}Id");
        builder.Property(static x => x.Right).HasConversion<TeamReferenceConverter>().HasColumnName(nameof(Team));
        builder.HasKey(static x => new { x.LeftId, x.Right });

        builder.HasOne<TPrincipalEntity>()
            .WithMany()
            .HasPrincipalKey(static x => x.Id)
            .HasForeignKey(static x => x.LeftId)
            .IsRequired();

        builder.HasIndex(static x => x.LeftId);

        return builder;
    }

    #endregion

    #region HasMany

    /// <summary>
    /// Configures many-to-many relationship with team references through join entity.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasManyTeams<TEntity, TId, TJoinEntity>(this EntityTypeBuilder<TEntity> builder, Action<EntityTypeBuilder<TEntity>>? builderAction = null)
        where TEntity : Entity<TId>
        where TId : EntityId<TId>
        where TJoinEntity : EntityTeam<TId>
    {
        var propertyName = nameof(Team).Pluralize(culture: ConventionHelper.DatabaseCulture);

        builder.Ignore(propertyName);

        builder.HasMany<TJoinEntity>()
            .WithOne()
            .HasForeignKey(static x => x.LeftId)
            .OnDelete(DeleteBehavior.Cascade);

        builderAction?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Configures many-to-many relationship with matches through join entity.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasManyMatches<TJoinEntity, TEntity, TId>(this EntityTypeBuilder<TEntity> builder, Action<EntityTypeBuilder<TEntity>>? builderAction = null)
        where TEntity : Entity<TId>
        where TId : EntityId<TId>
        where TJoinEntity : EntityMatch<TEntity, TId>
        => builder.HasMany<TJoinEntity, TEntity, Match, TId, MatchId>(builderAction);

    #endregion

    #region Owns

    /// <summary>
    /// Configures DisplayName value object ownership with standardized column mapping.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsDisplayName<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, DisplayName?>> expression, string? tableName = null)
        where TEntity : class
        => builder.OwnsOne(expression, x =>
        {
            if (!string.IsNullOrEmpty(tableName))
                x.ToTable(tableName);
            else
                x.ToTable<TEntity>();

            x.Property(static dn => dn.Name).HasColumnName(nameof(DisplayName.Name)).IsRequired();
            x.Property(static dn => dn.ShortName).HasColumnName(nameof(DisplayName.ShortName)).IsRequired();
        });

    /// <summary>
    /// Configures PeriodFormat value object ownership with prefixed column naming.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsPeriodFormat<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, PeriodFormat?>> expression, string? prefix = null)
        where TEntity : class
    {
        var expressionName = prefix ?? expression.GetMemberName();
        return builder.OwnsOne(expression, x =>
        {
            x.ToTable<TEntity>();
            x.Property(p => p.Number).HasColumnName($"{expressionName}{nameof(PeriodFormat.Number)}").IsRequired();
            x.Property(p => p.Duration).HasColumnName($"{expressionName}{nameof(PeriodFormat.Duration)}").IsRequired();
            x.Property(p => p.HalfTimeDuration).HasColumnName($"{expressionName}{nameof(PeriodFormat.HalfTimeDuration)}").IsRequired();
        });
    }

    /// <summary>
    /// Configures PeriodFormat value object ownership for owned entity types.
    /// </summary>
    public static OwnedNavigationBuilder<TOwner, TEntity> OwnsPeriodFormat<TOwner, TEntity>(this OwnedNavigationBuilder<TOwner, TEntity> builder, Expression<Func<TEntity, PeriodFormat?>> expression, string? prefix = null)
        where TOwner : class
        where TEntity : class
    {
        var expressionName = prefix ?? expression.GetMemberName();
        return builder.OwnsOne(expression, x =>
        {
            x.ToTable<TOwner>();
            x.Property(p => p.Number).HasColumnName($"{expressionName}{nameof(PeriodFormat.Number)}").IsRequired();
            x.Property(p => p.Duration).HasColumnName($"{expressionName}{nameof(PeriodFormat.Duration)}").IsRequired();
            x.Property(p => p.HalfTimeDuration).HasColumnName($"{expressionName}{nameof(PeriodFormat.HalfTimeDuration)}").IsRequired();
        });
    }

    /// <summary>
    /// Configures MatchFormat value object ownership with nested PeriodFormat configuration.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsMatchFormat<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, MatchFormat?>> expression)
        where TEntity : class
        => builder.OwnsOne(expression, static x =>
        {
            x.ToTable<TEntity>();
            x.OwnsPeriodFormat(static mf => mf.RegulationTime);
            x.OwnsPeriodFormat(static mf => mf.ExtraTime);
            x.Property(static mf => mf.NumberOfPenaltyShootouts).HasColumnName(nameof(MatchFormat.NumberOfPenaltyShootouts));
        });

    /// <summary>
    /// Configures MatchRules value object ownership with enum list conversion.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsMatchRules<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, MatchRules?>> expression)
        where TEntity : class
        => builder.OwnsOne(expression, static x =>
        {
            x.ToTable<TEntity>();
            x.Property(static r => r.AllowedCards).HasEnumListConversion().HasColumnName(nameof(MatchRules.AllowedCards));
        });

    /// <summary>
    /// Configures Address value object ownership with geographic coordinate support.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsAddress<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Address?>> expression, string? tableName = null)
        where TEntity : class
        => builder.OwnsOne(expression, x =>
        {
            if (!string.IsNullOrEmpty(tableName))
                x.ToTable(tableName);
            else
                x.ToTable<TEntity>();

            x.Property(static a => a.Street).HasColumnName(nameof(Address.Street));
            x.Property(static a => a.City).HasColumnName(nameof(Address.City));
            x.Property(static a => a.PostalCode).HasColumnName(nameof(Address.PostalCode));
            x.Property(static x => x.Country).HasConversion(new NullableEnumClassConverter<Country>());
            x.Property(static a => a.Country).HasColumnName(nameof(Address.Country));
            x.Property(static a => a.Latitude).HasColumnName(nameof(Address.Latitude));
            x.Property(static a => a.Longitude).HasColumnName(nameof(Address.Longitude));
        });

    /// <summary>
    /// Configures StandingLabel collection ownership with rank interval support.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsManyStandingLabels<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, IEnumerable<StandingLabel>?>> expression)
        where TEntity : class
        => builder.OwnsMany(expression, static x =>
        {
            x.ToTable<StandingLabel>(typeof(TEntity).Name);

            x.Property(static x => x.Color);
            x.Property(static x => x.Name).IsRequired();
            x.Property(static x => x.ShortName).IsRequired();
            x.Property(static x => x.Description);
            x.Property(static x => x.Order);

            x.OwnsOne(static x => x.Ranks, static y =>
            {
                y.ToTable<StandingLabel>(typeof(TEntity).Name);

                y.Property(static r => r.Start).HasColumnName(nameof(Interval<>.Start)).IsRequired();
                y.Property(static r => r.End).HasColumnName(nameof(Interval<>.End)).IsRequired();
            });
        });

    /// <summary>
    /// Configures StandingRuleSet value object ownership with complex converter integration.
    /// </summary>
    public static EntityTypeBuilder<TEntity> OwnsStandingRules<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, StandingRuleSet?>> expression)
        where TEntity : class
        => builder.OwnsOne(expression, static x =>
        {
            x.ToTable<StandingRuleSet>(typeof(TEntity).Name);

            x.Property(static x => x.PointsByOutcome).HasConversion(new DictionaryConverter<MatchResultType, int>()).HasColumnName(nameof(StandingRuleSet.PointsByOutcome));
            x.Property(static x => x.Columns).HasConversion(new StandingColumnListConverter()).HasColumnName(nameof(StandingRuleSet.Columns));
            x.Property(static x => x.Comparer).HasConversion(new StandingComparerConverter()).HasColumnName(nameof(StandingRuleSet.Comparer));
        });

    #endregion
}
