// -----------------------------------------------------------------------
// <copyright file="TieFormatJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Domain <see cref="TieFormat"/> to a JSON string for a PostgreSQL jsonb column.
/// </summary>
public sealed class TieFormatJsonConverter : ValueConverter<TieFormat?, string?>
{
    /// <summary>
    /// Gets the comparer that uses Domain equality and <see cref="TieFormat.Copy"/> for snapshots.
    /// </summary>
    public static ValueComparer<TieFormat?> Comparer { get; } = new(
        static (left, right) => left == right,
        static tieFormat => tieFormat == null ? 0 : tieFormat.GetHashCode(),
        static tieFormat => tieFormat == null ? null : tieFormat.Copy());

    /// <summary>
    /// Initializes a new instance of the <see cref="TieFormatJsonConverter"/> class.
    /// </summary>
    public TieFormatJsonConverter()
        : base(
            static tieFormat => tieFormat == null ? null : TieFormatJson.Serialize(tieFormat),
            static json => string.IsNullOrEmpty(json) ? null : TieFormatJson.Deserialize(json))
    {
    }
}
