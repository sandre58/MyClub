// -----------------------------------------------------------------------
// <copyright file="ForfeitMatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Composes an administrative forfeit <see cref="MatchResult"/> when an entry withdraws.
/// </summary>
public static class ForfeitMatchResult
{
    /// <summary>
    /// Builds a forfeit result: withdrawn side loses with policy loser goals; opponent gets winner goals.
    /// </summary>
    public static MatchResult ForWithdrawnSide(
        Match match,
        EntryId withdrawnEntryId,
        AdministrativeResultPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(policy);

        var withdrawnIsHome = match.HomeEntryId.Equals(withdrawnEntryId);
        if (!withdrawnIsHome && !match.AwayEntryId.Equals(withdrawnEntryId))
        {
            throw new ArgumentException(
                $"Match '{match.Id}' does not involve entry '{withdrawnEntryId}'.",
                nameof(withdrawnEntryId));
        }

        var homeGoals = withdrawnIsHome ? policy.ForfeitLoserGoals : policy.ForfeitWinnerGoals;
        var awayGoals = withdrawnIsHome ? policy.ForfeitWinnerGoals : policy.ForfeitLoserGoals;
        return new MatchResult(ResultType.Forfeit, new Score(homeGoals, awayGoals));
    }
}
