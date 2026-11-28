// -----------------------------------------------------------------------
// <copyright file="MatchResultJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Common;
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
        static result => Snapshot(result));

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResultJsonConverter"/> class.
    /// </summary>
    public MatchResultJsonConverter()
        : base(
            static result => result == null ? null : MatchResultJson.Serialize(result),
            static json => string.IsNullOrEmpty(json) ? null : MatchResultJson.Deserialize(json))
    {
    }

    private static MatchResult? Snapshot(MatchResult? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result.ConfrontationTiePenaltyShootout)
        {
            if (result.PenaltyShootoutScore is not { } shootout)
            {
                throw new DomainException(
                    "Confrontation tie penalty shootout requires a decisive shootout score.",
                    MatchErrorCodes.InvalidResult);
            }

            return MatchResult.CreateConfrontationTiePenaltyShootout(result.Score, result.ExtraTimePlayed, shootout);
        }

        return new MatchResult(result.Type, result.Score, result.ExtraTimePlayed, result.PenaltyShootoutScore);
    }
}
