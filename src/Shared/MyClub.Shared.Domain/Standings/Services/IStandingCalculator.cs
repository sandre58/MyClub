// -----------------------------------------------------------------------
// <copyright file="IStandingCalculator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Services;

/// <summary>
/// Interface for calculating team standings based on match results.
/// Provides functionality to compute standings tables for competitions using configurable rules and penalty points.
/// </summary>
public interface IStandingCalculator
{
    /// <summary>
    /// Calculates the standings for a group of teams based on their match results.
    /// </summary>
    /// <param name="teams">The teams to include in the standings calculation.</param>
    /// <param name="matches">The matches to consider when calculating standings.</param>
    /// <param name="rules">The rule set defining how points are awarded and how teams are compared.</param>
    /// <param name="penaltyPoints">Optional penalty points to be applied to specific teams. Can be null if no penalties apply.</param>
    /// <returns>A Standing object containing the calculated team rankings and statistics.</returns>
    Standing Calculate(IEnumerable<TeamReference> teams, IEnumerable<IMatch> matches, StandingRuleSet rules, IReadOnlyDictionary<TeamReference, int>? penaltyPoints = null);
}
