// -----------------------------------------------------------------------
// <copyright file="DefaultsBindingJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts <see cref="DefaultsBinding"/> to a PostgreSQL jsonb string.
/// </summary>
public sealed class DefaultsBindingJsonConverter : ValueConverter<DefaultsBinding, string>
{
    /// <summary>
    /// Gets the comparer using Domain equality and <see cref="DefaultsBinding.Copy"/>.
    /// </summary>
    public static ValueComparer<DefaultsBinding> Comparer { get; } = new(
        static (left, right) => left == null ? right == null : left.Equals(right),
        static binding => binding.GetHashCode(),
        static binding => binding.Copy());

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultsBindingJsonConverter"/> class.
    /// </summary>
    public DefaultsBindingJsonConverter()
        : base(
            static binding => DefaultsBindingJson.Serialize(binding),
            static json => DefaultsBindingJson.Deserialize(json))
    {
    }
}
