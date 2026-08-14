// -----------------------------------------------------------------------
// <copyright file="MatchResultJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Domain <see cref="MatchResult"/> to a JSON string for a PostgreSQL jsonb column.
/// </summary>
public sealed class MatchResultJsonConverter : ValueConverter<MatchResult?, string?>
{
    /// <summary>
    /// Gets the comparer that uses Domain record equality and reconstructs snapshots via the public constructor.
    /// </summary>
    public static ValueComparer<MatchResult?> Comparer { get; } = new(
        static (left, right) => left == right,
        static result => result == null ? 0 : result.GetHashCode(),
        static result => result == null
            ? null
            : new MatchResult(result.Type, result.Score, result.ExtraTimePlayed, result.PenaltyShootoutScore));

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResultJsonConverter"/> class.
    /// </summary>
    public MatchResultJsonConverter()
        : base(
            static result => result == null ? null : MatchResultJson.Serialize(result),
            static json => string.IsNullOrEmpty(json) ? null : MatchResultJson.Deserialize(json))
    {
    }
}
