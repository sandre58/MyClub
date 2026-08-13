// -----------------------------------------------------------------------
// <copyright file="Horizon.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Global scheduling horizon represented as half-open <c>[Start, End)</c>.
/// <para>
/// A17 métier: <c>H0 &gt; H1</c> ⇒ InvalidRequest. Mechanically this VO rejects
/// <c>start &gt; end</c> with <see cref="DomainException"/> (same pattern as other Scheduling VOs),
/// so that case never reaches <see cref="ScheduleGenerator.Generate"/> / <see cref="SchedulingResult"/>.
/// <c>H0 == H1</c> is a valid empty horizon (domains empty when Duration &gt; 0).
/// </para>
/// </summary>
public sealed record Horizon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Horizon"/> class.
    /// </summary>
    /// <param name="start">Inclusive horizon start.</param>
    /// <param name="end">Exclusive horizon end (must be greater than or equal to <paramref name="start"/>).</param>
    /// <exception cref="DomainException">Thrown when <paramref name="start"/> is greater than <paramref name="end"/> (A17; outside <see cref="SchedulingResult"/> trichotomy).</exception>
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
