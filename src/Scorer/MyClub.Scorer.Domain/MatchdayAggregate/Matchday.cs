// -----------------------------------------------------------------------
// <copyright file="Matchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.ValueObjects;

namespace MyClub.Scorer.Domain.MatchdayAggregate;

/// <summary>
/// Represents a matchday entity that groups matches played during a specific game week or round in a league competition.
/// A matchday typically contains multiple matches that are scheduled to be played around the same time period,
/// representing a "round" or "game week" in league competitions.
/// </summary>
public class Matchday : MatchScope<MatchdayId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Matchday() => DisplayName = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Matchday"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the matchday.</param>
    /// <param name="date">The scheduled date for this matchday.</param>
    /// <param name="name">The full name of the matchday (e.g., "Matchday 1", "Week 5").</param>
    /// <param name="shortName">The short name or abbreviation of the matchday (e.g., "MD1", "W5"). If not provided, it will be auto-generated.</param>
    private Matchday(MatchdayId id, DateTime date, string name, string? shortName = null)
        : base(id, date) => DisplayName = new(name, shortName);

    /// <summary>
    /// Creates a new matchday with the specified parameters.
    /// </summary>
    /// <param name="date">The scheduled date for this matchday.</param>
    /// <param name="name">The full name of the matchday (e.g., "Matchday 1", "Week 5").</param>
    /// <param name="shortName">The short name or abbreviation of the matchday (e.g., "MD1", "W5"). If not provided, it will be auto-generated from the full name.</param>
    /// <returns>A new <see cref="Matchday"/> instance.</returns>
    public static Matchday Create(DateTime date, string name, string? shortName = null) => new(MatchdayId.New(), date, name, shortName);

    /// <summary>
    /// Gets the display name of the matchday, including both full name and short name.
    /// This is used for presenting the matchday in various UI contexts.
    /// </summary>
    public DisplayName DisplayName { get; }

    /// <summary>
    /// Returns a string representation of the matchday using its display name.
    /// This provides a user-friendly representation of the matchday for logging and UI purposes.
    /// </summary>
    /// <returns>The display name of the matchday.</returns>
    public override string ToString() => DisplayName;
}
