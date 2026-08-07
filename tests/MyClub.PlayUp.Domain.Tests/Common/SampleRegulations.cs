// -----------------------------------------------------------------------
// <copyright file="SampleRegulations.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Tests.Common;

/// <summary>
/// Shared Regulation fixtures for Domain tests.
/// </summary>
internal static class SampleRegulations
{
    /// <summary>
    /// Standard amateur football regulation (2×45, 3-1-0, classic criteria, forfeit 3-0).
    /// Extra time and shootout are disabled.
    /// </summary>
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
