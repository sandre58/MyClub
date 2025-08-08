// -----------------------------------------------------------------------
// <copyright file="IChampionship.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.Primitives;

/// <summary>
/// Defines the contract for championship-style competitions that feature standings, matchdays, and ranking systems.
/// This interface extends <see cref="ITeamsContainer"/> to provide championship-specific functionality
/// for competitions where teams are ranked in a standings table based on their performance.
/// </summary>
public interface IChampionship : ITeamsContainer
{
    /// <summary>
    /// Gets the labels used to categorize different positions in the standings table.
    /// Labels provide semantic meaning to standings positions (e.g., "Champion", "European Qualification", "Relegation Zone").
    /// </summary>
    /// <value>
    /// A <see cref="StandingLabels"/> collection that maps standings positions to their descriptive labels.
    /// </value>
    StandingLabels Labels { get; }

    /// <summary>
    /// Gets the rule set used to calculate and order the standings table.
    /// This includes points awarded for different match results, tiebreaker criteria, and other ranking logic.
    /// </summary>
    /// <value>
    /// A <see cref="StandingRuleSet"/> that defines how teams are ranked and sorted in the standings.
    /// </value>
    StandingRuleSet StandingRules { get; }

    /// <summary>
    /// Gets the read-only collection of matchday identifiers that belong to this championship.
    /// Matchdays represent the scheduled rounds or game weeks in the competition.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="MatchdayId"/> representing the scheduled matchdays.
    /// </value>
    IReadOnlyCollection<MatchdayId> Matchdays { get; }

    /// <summary>
    /// Gets the read-only dictionary of penalty points applied to teams in this championship.
    /// Penalty points are typically deducted from a team's total points due to administrative infractions,
    /// rule violations, or other disciplinary actions.
    /// </summary>
    /// <value>
    /// A read-only dictionary where the key is the <see cref="TeamId"/> and the value is the number
    /// of penalty points (typically negative) applied to that team.
    /// </value>
    IReadOnlyDictionary<TeamId, int> PenaltyPoints { get; }
}
