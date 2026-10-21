// -----------------------------------------------------------------------
// <copyright file="StartMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: start an already-loaded Match (Scheduled → Live).
/// </summary>
/// <remarks>
/// Domain owns lifecycle transitions (<see cref="Match.Start"/>). Persistence: caller loads
/// the Match, invokes this use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// </remarks>
public static class StartMatch
{
    /// <summary>
    /// Starts <paramref name="match"/>.
    /// </summary>
    /// <param name="match">Match aggregate.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Match match, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);

        match.Start(clock);
    }
}
