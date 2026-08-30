// -----------------------------------------------------------------------
// <copyright file="RunningScoreJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a Domain <see cref="RunningScore"/> to a JSON string for a PostgreSQL jsonb column.
/// </summary>
public sealed class RunningScoreJsonConverter : ValueConverter<RunningScore?, string?>
{
    private static readonly JsonSerializerOptions Options = new();

    /// <summary>
    /// Gets the comparer that uses Domain struct equality and reconstructs snapshots via the public constructor.
    /// </summary>
    public static ValueComparer<RunningScore?> Comparer { get; } = new(
        static (left, right) => left == right,
        static score => score == null ? 0 : score.GetHashCode(),
        static score => score == null
            ? null
            : new RunningScore(score.Value.HomeGoals, score.Value.AwayGoals));

    /// <summary>
    /// Initializes a new instance of the <see cref="RunningScoreJsonConverter"/> class.
    /// </summary>
    public RunningScoreJsonConverter()
        : base(
            static score => score == null ? null : Serialize(score.Value),
            static json => string.IsNullOrEmpty(json) ? null : Deserialize(json))
    {
    }

    private static string Serialize(RunningScore score) =>
        JsonSerializer.Serialize(new RunningScoreDocument(score.HomeGoals, score.AwayGoals), Options);

    private static RunningScore Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<RunningScoreDocument>(json, Options)
            ?? throw new InvalidOperationException("RunningScore JSON is empty.");
        return new RunningScore(document.HomeGoals, document.AwayGoals);
    }

    private sealed record RunningScoreDocument(int HomeGoals, int AwayGoals);
}
