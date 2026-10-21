// -----------------------------------------------------------------------
// <copyright file="ArchiveCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: archive a completed competition.
/// </summary>
/// <remarks>
/// Domain owns <c>Completed → Archived</c>. Not an alias of Complete.
/// </remarks>
public static class ArchiveCompetition
{
    /// <summary>
    /// Archives the competition when Domain allows it.
    /// </summary>
    /// <param name="competition">Target competition (must be Completed).</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);

        competition.Archive(clock);
    }
}
