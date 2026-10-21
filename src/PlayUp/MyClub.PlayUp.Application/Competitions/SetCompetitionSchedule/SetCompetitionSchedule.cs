// -----------------------------------------------------------------------
// <copyright file="SetCompetitionSchedule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: set or clear declared competition schedule dates.
/// </summary>
public static class SetCompetitionSchedule
{
    /// <summary>
    /// Sets declared schedule; both null clears.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="scheduledStart">Declared start.</param>
    /// <param name="scheduledEnd">Declared end.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(
        Competition competition,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.SetSchedule(scheduledStart, scheduledEnd, clock);
    }
}
