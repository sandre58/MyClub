// -----------------------------------------------------------------------
// <copyright file="RemoveRecordedGoal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: remove a nominative goal attribution.
/// </summary>
public static class RemoveRecordedGoal
{
    /// <summary>
    /// Removes a recorded goal from the match journal.
    /// </summary>
    public static void Execute(Match match, GoalId goalId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.RemoveRecordedGoal(goalId, clock);
    }
}
