// -----------------------------------------------------------------------
// <copyright file="MatchDuration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Regulation-time duration of a match (fixed periods; no dynamic period collection).
/// Durations are expressed in whole minutes.
/// </summary>
public sealed record MatchDuration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchDuration"/> class.
    /// </summary>
    /// <param name="durationPerPeriod">Minutes per regulation period (&gt; 0).</param>
    /// <param name="numberOfPeriods">Number of regulation periods (≥ 1).</param>
    /// <param name="halfTimeDuration">Half-time break in minutes (≥ 0).</param>
    public MatchDuration(int durationPerPeriod, int numberOfPeriods, int halfTimeDuration)
    {
        if (durationPerPeriod <= 0)
        {
            throw new DomainException(
                "Duration per period must be greater than 0 minutes.",
                RulesErrorCodes.MatchDurationInvalid);
        }

        if (numberOfPeriods < 1)
        {
            throw new DomainException(
                "Number of periods must be at least 1.",
                RulesErrorCodes.MatchDurationInvalid);
        }

        if (halfTimeDuration < 0)
        {
            throw new DomainException(
                "Half-time duration cannot be negative.",
                RulesErrorCodes.MatchDurationInvalid);
        }

        DurationPerPeriod = durationPerPeriod;
        NumberOfPeriods = numberOfPeriods;
        HalfTimeDuration = halfTimeDuration;
    }

    /// <summary>
    /// Gets the duration of each regulation period in minutes.
    /// </summary>
    public int DurationPerPeriod { get; }

    /// <summary>
    /// Gets the number of regulation periods.
    /// </summary>
    public int NumberOfPeriods { get; }

    /// <summary>
    /// Gets the half-time break duration in minutes.
    /// </summary>
    public int HalfTimeDuration { get; }
}
