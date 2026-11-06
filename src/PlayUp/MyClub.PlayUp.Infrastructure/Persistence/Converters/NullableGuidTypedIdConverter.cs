// -----------------------------------------------------------------------
// <copyright file="NullableGuidTypedIdConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a nullable Guid-backed Domain typed identifier to and from <see cref="Guid"/> for EF Core.
/// </summary>
/// <typeparam name="TId">A typed id with a public <see cref="Guid"/> constructor and a public <c>Value</c> property.</typeparam>
public sealed class NullableGuidTypedIdConverter<TId> : ValueConverter<TId?, Guid?>
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableGuidTypedIdConverter{TId}"/> class.
    /// </summary>
    public NullableGuidTypedIdConverter()
        : base(CreateToProviderExpression(), CreateFromProviderExpression())
    {
    }

    private static Expression<Func<TId?, Guid?>> CreateToProviderExpression()
    {
        var id = Expression.Parameter(typeof(TId?), "id");
        var hasValue = Expression.Property(id, nameof(Nullable<TId>.HasValue));
        var value = Expression.Property(Expression.Property(id, nameof(Nullable<TId>.Value)), GetValueProperty());
        var body = Expression.Condition(
            hasValue,
            Expression.Convert(value, typeof(Guid?)),
            Expression.Constant(null, typeof(Guid?)));
        return Expression.Lambda<Func<TId?, Guid?>>(body, id);
    }

    private static Expression<Func<Guid?, TId?>> CreateFromProviderExpression()
    {
        var value = Expression.Parameter(typeof(Guid?), "value");
        var hasValue = Expression.Property(value, nameof(Nullable<Guid>.HasValue));
        var guid = Expression.Property(value, nameof(Nullable<Guid>.Value));
        var constructed = Expression.Convert(
            Expression.New(GetGuidConstructor(), guid),
            typeof(TId?));
        var body = Expression.Condition(
            hasValue,
            constructed,
            Expression.Constant(null, typeof(TId?)));
        return Expression.Lambda<Func<Guid?, TId?>>(body, value);
    }

    private static PropertyInfo GetValueProperty()
    {
        var property = typeof(TId).GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        return property is null || property.PropertyType != typeof(Guid)
            ? throw new InvalidOperationException(
                $"{typeof(TId).Name} must expose a public instance property named Value of type {nameof(Guid)}.")
            : property;
    }

    private static ConstructorInfo GetGuidConstructor() =>
        typeof(TId).GetConstructor([typeof(Guid)])
        ?? throw new InvalidOperationException(
            $"{typeof(TId).Name} must expose a public constructor that accepts a {nameof(Guid)}.");
}
