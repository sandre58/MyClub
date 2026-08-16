// -----------------------------------------------------------------------
// <copyright file="WithdrawEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: withdraw an entry (soft leave; history kept).
/// </summary>
public static class WithdrawEntry
{
    /// <summary>
    /// Withdraws an entry. Allowed in Draft/Ready and after Start per Domain.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.WithdrawEntry(entryId, clock);
    }
}
