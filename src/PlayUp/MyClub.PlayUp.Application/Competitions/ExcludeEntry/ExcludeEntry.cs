// -----------------------------------------------------------------------
// <copyright file="ExcludeEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: exclude an entry (organizer decision; Draft/Ready only).
/// </summary>
public static class ExcludeEntry
{
    /// <summary>
    /// Excludes an entry before the competition starts.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.ExcludeEntry(entryId, clock);
    }
}
