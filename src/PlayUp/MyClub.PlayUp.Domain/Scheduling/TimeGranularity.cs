// -----------------------------------------------------------------------
// <copyright file="TimeGranularity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Global start-time grid step for freely generated starts (whole minutes).
/// </summary>
public sealed record TimeGranularity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeGranularity"/> class.
    /// </summary>
    /// <param name="minutes">Grid step in whole minutes (&gt; 0).</param>
    public TimeGranularity(int minutes)
    {
        if (minutes <= 0)
        {
            throw new DomainException(
                "Time granularity must be greater than 0 minutes.",
                SchedulingErrorCodes.GranularityInvalid);
        }

        Minutes = minutes;
    }

    /// <summary>
    /// Gets the grid step in whole minutes.
    /// </summary>
    public int Minutes { get; }

    /// <summary>
    /// Gets the grid step as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan AsTimeSpan() => TimeSpan.FromMinutes(Minutes);
}
