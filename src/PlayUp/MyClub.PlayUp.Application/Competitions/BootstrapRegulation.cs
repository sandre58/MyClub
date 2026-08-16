// -----------------------------------------------------------------------
// <copyright file="BootstrapRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application-default <see cref="Regulation"/> for Slice 1 CreateCompetition bootstrap.
/// </summary>
/// <remarks>
/// Domain requires a Regulation at <c>Competition.Create</c>. Create uses this standard football
/// amateur baseline; organizers replace it via Slice 2 <c>ReplaceRegulation</c> when needed.
/// Not a Domain concept and not a product Catalog template.
/// </remarks>
public static class BootstrapRegulation
{
    /// <summary>
    /// Builds the Slice 1 default regulation (2–64 teams, 2×45, 3/1/0 points, classic tie-breakers).
    /// </summary>
    /// <returns>A new <see cref="Regulation"/> instance.</returns>
    public static Regulation Standard() =>
        new(
            new EntryRules(minimumTeams: 2, maximumTeams: 64),
            new MatchRules(
                new MatchDuration(durationPerPeriod: 45, numberOfPeriods: 2, halfTimeDuration: 15),
                new AdministrativeResultPolicy(forfeitWinnerGoals: 3, forfeitLoserGoals: 0)),
            new StandingRules(
                new PointsPolicy(winPoints: 3, drawPoints: 1, lossPoints: 0),
                [
                    RankingCriterion.Points,
                    RankingCriterion.GoalDifference,
                    RankingCriterion.GoalsFor,
                    RankingCriterion.HeadToHead
                ]));
}
