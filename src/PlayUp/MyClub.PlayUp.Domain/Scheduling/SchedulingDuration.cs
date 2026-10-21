// -----------------------------------------------------------------------
// <copyright file="SchedulingDuration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Occupation duration of a match for scheduling (whole minutes). Distinct from regulation <c>MatchDuration</c>.
/// </summary>
public sealed record SchedulingDuration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingDuration"/> class.
    /// </summary>
    /// <param name="minutes">Duration in whole minutes (&gt; 0).</param>
    public SchedulingDuration(int minutes)
    {
        if (minutes <= 0)
        {
            throw new DomainException(
                "Scheduling duration must be greater than 0 minutes.",
                SchedulingErrorCodes.DurationInvalid);
        }

        Minutes = minutes;
    }

    /// <summary>
    /// Gets the duration in whole minutes.
    /// </summary>
    public int Minutes { get; }

    /// <summary>
    /// Gets the duration as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan AsTimeSpan() => TimeSpan.FromMinutes(Minutes);
}
