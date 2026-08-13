// -----------------------------------------------------------------------
// <copyright file="Precedence.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Required precedence: successor start ≥ predecessor end + minimum delay.
/// </summary>
public sealed record Precedence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Precedence"/> class.
    /// </summary>
    public Precedence(MatchId predecessorMatchId, MatchId successorMatchId, int minimumDelayMinutes)
    {
        if (predecessorMatchId.Equals(successorMatchId))
        {
            throw new DomainException(
                "Precedence cannot reference the same match twice.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        if (minimumDelayMinutes < 0)
        {
            throw new DomainException(
                "Precedence minimum delay cannot be negative.",
                SchedulingErrorCodes.ConstraintInvalid);
        }

        PredecessorMatchId = predecessorMatchId;
        SuccessorMatchId = successorMatchId;
        MinimumDelayMinutes = minimumDelayMinutes;
    }

    /// <summary>
    /// Gets the predecessor match identity.
    /// </summary>
    public MatchId PredecessorMatchId { get; }

    /// <summary>
    /// Gets the successor match identity.
    /// </summary>
    public MatchId SuccessorMatchId { get; }

    /// <summary>
    /// Gets the minimum delay in whole minutes.
    /// </summary>
    public int MinimumDelayMinutes { get; }

    /// <summary>
    /// Gets the minimum delay as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan MinimumDelay => TimeSpan.FromMinutes(MinimumDelayMinutes);
}
