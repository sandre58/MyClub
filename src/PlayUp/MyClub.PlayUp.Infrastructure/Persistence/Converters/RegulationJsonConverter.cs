// -----------------------------------------------------------------------
// <copyright file="RegulationJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Domain <see cref="Regulation"/> to a JSON string for a PostgreSQL jsonb column.
/// </summary>
public sealed class RegulationJsonConverter : ValueConverter<Regulation, string>
{
    /// <summary>
    /// Gets the comparer that uses Domain equality and <see cref="Regulation.Copy"/> for snapshots.
    /// </summary>
    public static ValueComparer<Regulation> Comparer { get; } = new(
        static (left, right) => left == right,
        static regulation => regulation.GetHashCode(),
        static regulation => regulation.Copy());

    /// <summary>
    /// Initializes a new instance of the <see cref="RegulationJsonConverter"/> class.
    /// </summary>
    public RegulationJsonConverter()
        : base(
            static regulation => RegulationJson.Serialize(regulation),
            static json => RegulationJson.Deserialize(json))
    {
    }
}
