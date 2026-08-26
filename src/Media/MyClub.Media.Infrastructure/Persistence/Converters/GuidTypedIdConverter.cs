// -----------------------------------------------------------------------
// <copyright file="GuidTypedIdConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyClub.Media.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Guid-backed typed identifier to and from <see cref="Guid"/> for EF Core.
/// </summary>
/// <typeparam name="TId">A typed id with a public <see cref="Guid"/> constructor and a public <c>Value</c> property.</typeparam>
internal sealed class GuidTypedIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidTypedIdConverter{TId}"/> class.
    /// </summary>
    public GuidTypedIdConverter()
        : base(CreateToProviderExpression(), CreateFromProviderExpression())
    {
    }

    private static Expression<Func<TId, Guid>> CreateToProviderExpression()
    {
        var id = Expression.Parameter(typeof(TId), "id");
        var value = Expression.Property(id, GetValueProperty());
        return Expression.Lambda<Func<TId, Guid>>(value, id);
    }

    private static Expression<Func<Guid, TId>> CreateFromProviderExpression()
    {
        var value = Expression.Parameter(typeof(Guid), "value");
        return Expression.Lambda<Func<Guid, TId>>(Expression.New(GetGuidConstructor(), value), value);
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
