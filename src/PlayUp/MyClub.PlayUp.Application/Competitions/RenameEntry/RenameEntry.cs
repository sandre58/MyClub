// -----------------------------------------------------------------------
// <copyright file="RenameEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: rename a competition entry display name.
/// </summary>
public static class RenameEntry
{
    /// <summary>
    /// Renames an entry on the competition.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, EntryId entryId, string displayName, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.RenameEntry(entryId, displayName, clock);
    }
}
