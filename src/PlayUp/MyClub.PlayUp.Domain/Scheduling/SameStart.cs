// -----------------------------------------------------------------------
// <copyright file="SameStart.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Required same-start relation between two distinct matches (resources independent).
/// </summary>
public sealed record SameStart
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SameStart"/> class.
    /// </summary>
    public SameStart(MatchId matchA, MatchId matchB)
    {
        if (matchA.Equals(matchB))
        {
            throw new DomainException(
                "SameStart requires two distinct matches.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        MatchA = matchA;
        MatchB = matchB;
    }

    /// <summary>
    /// Gets the first match identity.
    /// </summary>
    public MatchId MatchA { get; }

    /// <summary>
    /// Gets the second match identity.
    /// </summary>
    public MatchId MatchB { get; }
}
