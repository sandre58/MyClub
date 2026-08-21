// -----------------------------------------------------------------------
// <copyright file="StartCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: start a competition (Ready → Running).
/// </summary>
/// <remarks>
/// Domain owns preconditions and the transition. Persistence: caller loads the competition,
/// invokes this use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// </remarks>
public static class StartCompetition
{
    /// <summary>
    /// Starts <paramref name="competition"/> when Domain allows it.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);

        competition.Start(clock);
    }
}
