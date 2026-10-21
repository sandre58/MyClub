// -----------------------------------------------------------------------
// <copyright file="ResultGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Builds Domain <see cref="MatchResult"/> values (never Read DTOs).
/// </summary>
public static class ResultGenerator
{
    /// <summary>
    /// Creates a played result from home/away goals.
    /// </summary>
    /// <param name="homeGoals">Home goals.</param>
    /// <param name="awayGoals">Away goals.</param>
    /// <returns>Domain match result.</returns>
    public static MatchResult Played(int homeGoals, int awayGoals) =>
        new(ResultType.Played, new Score(homeGoals, awayGoals));
}
