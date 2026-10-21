// -----------------------------------------------------------------------
// <copyright file="FinishMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: finish an already-loaded Match with a result (Live → Finished).
/// </summary>
/// <remarks>
/// Domain owns lifecycle and result recording atomically (<see cref="Match.Finish"/>).
/// Persistence: caller loads the Match, invokes this use case, then calls
/// <c>IUnitOfWork.SaveChangesAsync</c> once. Do not introduce a separate RecordMatchResult use case.
/// </remarks>
public static class FinishMatch
{
    /// <summary>
    /// Finishes <paramref name="match"/> with <paramref name="result"/>.
    /// </summary>
    /// <param name="match">Match aggregate.</param>
    /// <param name="result">Domain match result.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Match match, MatchResult result, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(clock);

        match.Finish(result, clock);
    }
}
