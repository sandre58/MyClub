// -----------------------------------------------------------------------
// <copyright file="StageRegulationJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Domain <see cref="StageRegulation"/> to a JSON string for a PostgreSQL jsonb column.
/// </summary>
public sealed class StageRegulationJsonConverter : ValueConverter<StageRegulation, string>
{
    /// <summary>
    /// Gets the comparer that uses Domain equality and <see cref="StageRegulation.Copy"/> for snapshots.
    /// </summary>
    public static ValueComparer<StageRegulation> Comparer { get; } = new(
        static (left, right) => left == right,
        static regulation => regulation.GetHashCode(),
        static regulation => regulation.Copy());

    /// <summary>
    /// Initializes a new instance of the <see cref="StageRegulationJsonConverter"/> class.
    /// </summary>
    public StageRegulationJsonConverter()
        : base(
            static regulation => StageRegulationJson.Serialize(regulation),
            static json => StageRegulationJson.Deserialize(json))
    {
    }
}
