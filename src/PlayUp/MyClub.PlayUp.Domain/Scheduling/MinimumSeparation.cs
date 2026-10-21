// -----------------------------------------------------------------------
// <copyright file="MinimumSeparation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Required minimum separation between two match occupations (interval semantics; S=0 = non-overlap).
/// </summary>
public sealed record MinimumSeparation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinimumSeparation"/> class.
    /// </summary>
    public MinimumSeparation(MatchId matchA, MatchId matchB, int minutes)
    {
        if (matchA.Equals(matchB))
        {
            throw new DomainException(
                "Minimum separation requires two distinct matches.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        if (minutes < 0)
        {
            throw new DomainException(
                "Minimum separation cannot be negative.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        MatchA = matchA;
        MatchB = matchB;
        Minutes = minutes;
    }

    /// <summary>
    /// Gets the first match identity.
    /// </summary>
    public MatchId MatchA { get; }

    /// <summary>
    /// Gets the second match identity.
    /// </summary>
    public MatchId MatchB { get; }

    /// <summary>
    /// Gets the separation in whole minutes.
    /// </summary>
    public int Minutes { get; }

    /// <summary>
    /// Gets the separation as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan AsTimeSpan() => TimeSpan.FromMinutes(Minutes);
}
