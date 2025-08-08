// -----------------------------------------------------------------------
// <copyright file="RoundStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.ValueObjects;

namespace MyClub.Scorer.Domain.RoundAggregate;

/// <summary>
/// Represents a stage within a tournament round that groups matches played during a specific phase or leg.
/// A round stage organizes matches within complex round formats such as home-and-away legs,
/// best-of-X series, or multiple match phases within a single round.
/// </summary>
public class RoundStage : MatchScope<RoundStageId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private RoundStage() => DisplayName = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundStage"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the round stage.</param>
    /// <param name="date">The scheduled date for this round stage.</param>
    /// <param name="name">The full name of the round stage (e.g., "First Leg", "Game 3").</param>
    /// <param name="shortName">The short name or abbreviation of the round stage (e.g., "1st", "G3"). If not provided, it will be auto-generated.</param>
    private RoundStage(RoundStageId id, DateTime date, string name, string? shortName = null)
        : base(id, date) => DisplayName = new(name, shortName);

    /// <summary>
    /// Creates a new round stage with the specified parameters.
    /// </summary>
    /// <param name="date">The scheduled date for this round stage.</param>
    /// <param name="name">The full name of the round stage (e.g., "First Leg", "Game 3").</param>
    /// <param name="shortName">The short name or abbreviation of the round stage (e.g., "1st", "G3"). If not provided, it will be auto-generated from the full name.</param>
    /// <returns>A new <see cref="RoundStage"/> instance.</returns>
    public static RoundStage Create(DateTime date, string name, string? shortName = null) => new(RoundStageId.New(), date, name, shortName);

    /// <summary>
    /// Gets the display name of the round stage, including both full name and short name.
    /// This is used for presenting the round stage in various UI contexts and match organization.
    /// </summary>
    public DisplayName DisplayName { get; }

    /// <summary>
    /// Returns a string representation of the round stage using its display name.
    /// This provides a user-friendly representation of the round stage for logging and UI purposes.
    /// </summary>
    /// <returns>The display name of the round stage.</returns>
    public override string ToString() => DisplayName;
}
