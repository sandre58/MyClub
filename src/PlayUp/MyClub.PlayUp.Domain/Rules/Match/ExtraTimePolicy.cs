// -----------------------------------------------------------------------
// <copyright file="ExtraTimePolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Extra-time parameters when present on <see cref="MatchRules"/>.
/// Presence means extra time is enabled; there is no <c>Enabled</c> flag.
/// Durations are expressed in whole minutes.
/// </summary>
public sealed record ExtraTimePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtraTimePolicy"/> class.
    /// </summary>
    /// <param name="durationPerPeriod">Minutes per extra-time period (&gt; 0).</param>
    /// <param name="numberOfPeriods">Number of extra-time periods (≥ 1).</param>
    public ExtraTimePolicy(int durationPerPeriod, int numberOfPeriods)
    {
        if (durationPerPeriod <= 0)
        {
            throw new DomainException(
                "Extra-time duration per period must be greater than 0 minutes.",
                RulesErrorCodes.ExtraTimePolicyInvalid);
        }

        if (numberOfPeriods < 1)
        {
            throw new DomainException(
                "Extra-time number of periods must be at least 1.",
                RulesErrorCodes.ExtraTimePolicyInvalid);
        }

        DurationPerPeriod = durationPerPeriod;
        NumberOfPeriods = numberOfPeriods;
    }

    /// <summary>
    /// Gets the duration of each extra-time period in minutes.
    /// </summary>
    public int DurationPerPeriod { get; }

    /// <summary>
    /// Gets the number of extra-time periods.
    /// </summary>
    public int NumberOfPeriods { get; }
}
