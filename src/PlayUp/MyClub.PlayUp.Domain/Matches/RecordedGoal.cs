// -----------------------------------------------------------------------
// <copyright file="RecordedGoal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Nominative goal attribution recorded on a match — distinct from <see cref="RunningScore"/> and <see cref="MatchResult.Score"/>.
/// </summary>
[DebuggerDisplay("{ScorerMemberId} → {CreditedSide} ({Id})")]
public sealed class RecordedGoal : Entity<GoalId>
{
    internal RecordedGoal(
        GoalId id,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId)
        : base(id)
    {
        EnsureDefinedSide(creditedSide);
        ScorerMemberId = scorerMemberId;
        CreditedSide = creditedSide;
        AssisterMemberId = assisterMemberId;
    }

    /// <summary>
    /// Gets the scorer member identity (must be on the match composition).
    /// </summary>
    public MemberId ScorerMemberId { get; private set; }

    /// <summary>
    /// Gets the side credited with the goal (explicit; not derived from the scorer's sheet side).
    /// </summary>
    public Side CreditedSide { get; private set; }

    /// <summary>
    /// Gets the optional assister member identity (forbidden on own goals; never equal to the scorer).
    /// </summary>
    public MemberId? AssisterMemberId { get; private set; }

    internal void Correct(MemberId scorerMemberId, Side creditedSide, MemberId? assisterMemberId)
    {
        EnsureDefinedSide(creditedSide);
        ScorerMemberId = scorerMemberId;
        CreditedSide = creditedSide;
        AssisterMemberId = assisterMemberId;
    }

    private static void EnsureDefinedSide(Side side)
    {
        if (!Enum.IsDefined(side))
        {
            throw new DomainException(
                $"Unknown match side '{side}'.",
                MatchErrorCodes.InvalidSide);
        }
    }
}
