// -----------------------------------------------------------------------
// <copyright file="Horizon.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Global closed scheduling horizon represented as half-open <c>[Start, End)</c>.
/// </summary>
public sealed record Horizon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Horizon"/> class.
    /// </summary>
    /// <param name="start">Inclusive horizon start.</param>
    /// <param name="end">Exclusive horizon end (must be greater than or equal to <paramref name="start"/>).</param>
    public Horizon(DateTimeOffset start, DateTimeOffset end)
    {
        if (start > end)
        {
            throw new DomainException(
                "Horizon start must not be greater than end.",
                SchedulingErrorCodes.HorizonInvalid);
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the inclusive horizon start.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets the exclusive horizon end.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// Returns whether an occupation is fully contained in the horizon.
    /// </summary>
    public bool ContainsOccupation(DateTimeOffset occupationStart, DateTimeOffset occupationEnd) =>
        occupationStart >= Start && occupationEnd <= End;
}
