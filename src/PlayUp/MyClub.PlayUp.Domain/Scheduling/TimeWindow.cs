// -----------------------------------------------------------------------
// <copyright file="TimeWindow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Half-open absolute time window <c>[Start, End)</c>.
/// </summary>
public sealed record TimeWindow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeWindow"/> class.
    /// </summary>
    /// <param name="start">Inclusive start instant.</param>
    /// <param name="end">Exclusive end instant (must be greater than <paramref name="start"/>).</param>
    public TimeWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new DomainException(
                "Time window start must be strictly less than end.",
                SchedulingErrorCodes.TimeWindowInvalid);
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the inclusive start instant.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets the exclusive end instant.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// Returns whether <paramref name="occupationStart"/>..<paramref name="occupationEnd"/> is fully contained in this window.
    /// </summary>
    public bool ContainsOccupation(DateTimeOffset occupationStart, DateTimeOffset occupationEnd) =>
        occupationStart >= Start && occupationEnd <= End;
}
