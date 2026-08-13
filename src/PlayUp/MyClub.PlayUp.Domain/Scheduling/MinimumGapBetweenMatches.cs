// -----------------------------------------------------------------------
// <copyright file="MinimumGapBetweenMatches.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Required minimum gap between successive occupations on the same resource (whole minutes, ≥ 0).
/// </summary>
public sealed record MinimumGapBetweenMatches
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinimumGapBetweenMatches"/> class.
    /// </summary>
    /// <param name="minutes">Gap in whole minutes (≥ 0). Zero allows adjacent occupations.</param>
    public MinimumGapBetweenMatches(int minutes)
    {
        if (minutes < 0)
        {
            throw new DomainException(
                "Minimum gap cannot be negative.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        Minutes = minutes;
    }

    /// <summary>
    /// Gets the gap in whole minutes.
    /// </summary>
    public int Minutes { get; }

    /// <summary>
    /// Gets the gap as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan AsTimeSpan() => TimeSpan.FromMinutes(Minutes);
}
