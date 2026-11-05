// -----------------------------------------------------------------------
// <copyright file="CorrectMatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: replace the official result on a finished match without changing lifecycle status.
/// </summary>
public static class CorrectMatchResult
{
    /// <summary>
    /// Corrects <paramref name="match"/> result via <see cref="Match.CorrectMatchResult"/>.
    /// </summary>
    public static void Execute(Match match, MatchResult result, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(clock);

        match.CorrectMatchResult(result, clock);
    }
}
