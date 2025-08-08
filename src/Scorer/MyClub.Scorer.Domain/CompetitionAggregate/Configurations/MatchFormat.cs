// -----------------------------------------------------------------------
// <copyright file="MatchFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

/// <summary>
/// Represents the format configuration for a football match, including timing and overtime rules.
/// This value object defines how long a match lasts and what happens in case of a draw.
/// </summary>
/// <param name="RegulationTime">The configuration for regular playing time.</param>
/// <param name="ExtraTime">The configuration for extra time periods. Null if extra time is not used.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null or 0 if shootouts are not used.</param>
public record MatchFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private MatchFormat()
        : this(PeriodFormat.Default) { }

    /// <summary>
    /// Gets the default match format: 90 minutes (2 × 45) with no extra time or penalty shootouts.
    /// This is the standard format used in most league competitions.
    /// </summary>
    public static readonly MatchFormat Default = new(PeriodFormat.Default);

    /// <summary>
    /// Gets a match format that doesn't allow draws: 90 minutes + 30 minutes extra time + 5 penalty shootouts.
    /// This format is typically used in knockout competitions where a winner must be determined.
    /// </summary>
    public static readonly MatchFormat NoDraw = new(PeriodFormat.Default, PeriodFormat.ExtraTime, 5);

    /// <summary>
    /// Gets a value indicating whether extra time is enabled for this match format.
    /// </summary>
    public bool ExtraTimeIsEnabled => ExtraTime is not null;

    /// <summary>
    /// Gets a value indicating whether penalty shootouts are enabled for this match format.
    /// </summary>
    public bool ShootoutIsEnabled => NumberOfPenaltyShootouts > 0;

    /// <summary>
    /// Calculates the total duration of the match including regulation time and extra time.
    /// </summary>
    /// <param name="withHalfTime">Whether to include half-time breaks in the calculation.</param>
    /// <returns>The total <see cref="TimeSpan"/> duration of the match.</returns>
    public TimeSpan GetFullTime(bool withHalfTime = true) => RegulationTime.GetFullTime(withHalfTime) + (ExtraTime?.GetFullTime(withHalfTime) ?? TimeSpan.Zero);

    /// <summary>
    /// Returns a string representation of the match format including all timing details.
    /// </summary>
    /// <returns>A formatted string describing the match format.</returns>
    public override string ToString()
    {
        var str = new StringBuilder(RegulationTime.ToString());

        if (ExtraTimeIsEnabled)
            _ = str.Append(CultureInfo.CurrentCulture, $" ({ExtraTime})");

        if (ShootoutIsEnabled)
            _ = str.Append(CultureInfo.CurrentCulture, $" + {NumberOfPenaltyShootouts} penalty shootouts");

        return str.ToString();
    }
}
