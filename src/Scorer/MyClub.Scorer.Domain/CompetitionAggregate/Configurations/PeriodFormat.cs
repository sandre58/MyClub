// -----------------------------------------------------------------------
// <copyright file="PeriodFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using JetBrains.Annotations;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

/// <summary>
/// Represents the format configuration for a period of play in a football match.
/// This value object defines the structure of playing time periods, including the number of periods,
/// duration of each period, and half-time breaks.
/// </summary>
/// <param name="Number">The number of periods (e.g., 2 for regular football, 4 for some indoor variants).</param>
/// <param name="Duration">The duration of each individual period.</param>
/// <param name="HalfTimeDuration">The duration of the break between periods. Null if no break is used.</param>
public record PeriodFormat(int Number, TimeSpan Duration, TimeSpan? HalfTimeDuration = null)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private PeriodFormat()
        : this(2, TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(15)) { }

    /// <summary>
    /// Gets the default period format: 2 periods of 45 minutes with 15-minute half-time.
    /// This is the standard format used in professional football matches.
    /// </summary>
    public static readonly PeriodFormat Default = new(2, 45.Minutes(), 15.Minutes());

    /// <summary>
    /// Gets the extra time period format: 2 periods of 15 minutes with 5-minute break.
    /// This format is used for extra time periods in knockout competitions.
    /// </summary>
    public static readonly PeriodFormat ExtraTime = new(2, 15.Minutes(), 5.Minutes());

    /// <summary>
    /// Calculates the total duration including all periods and breaks.
    /// </summary>
    /// <param name="withHalfTime">Whether to include half-time breaks in the total duration calculation.</param>
    /// <returns>The total <see cref="TimeSpan"/> duration including all periods and optional breaks.</returns>
    public TimeSpan GetFullTime(bool withHalfTime = true) => (Number * Duration) + ((Number - 1) * (withHalfTime && HalfTimeDuration.HasValue ? HalfTimeDuration.Value : TimeSpan.Zero));

    /// <summary>
    /// Returns a string representation of the period format in a compact notation.
    /// </summary>
    /// <returns>A formatted string in the format "NumberxDuration'" (e.g., "2x45'").</returns>
    public override string ToString() => $"{Number}x{Duration.TotalMinutes}'";
}
