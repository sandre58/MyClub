// -----------------------------------------------------------------------
// <copyright file="RoundRobinStrategy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchdayAggregate.Services;

/// <summary>
/// Implements a round-robin scheduling strategy where every team plays against every other team
/// a specified number of times. This strategy is commonly used in league competitions where
/// comprehensive head-to-head results are desired for fair ranking determination.
/// </summary>
/// <param name="matchesPerPair">The number of times each pair of teams should play against each other.</param>
/// <param name="invertTeamsByStage">Array indicating whether to invert home/away teams for each stage in the series.</param>
public class RoundRobinStrategy(int matchesPerPair, bool[] invertTeamsByStage) : IMatchdayScheduleStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinStrategy"/> class with automatic home/away inversion.
    /// </summary>
    /// <param name="matchesPerPair">The number of times each pair of teams should play against each other.</param>
    public RoundRobinStrategy(int matchesPerPair)
        : this(matchesPerPair, [.. Enumerable.Range(1, matchesPerPair).Select(static x => x % 2 == 0)]) { }

    /// <summary>
    /// Gets the default round-robin strategy with double round-robin configuration.
    /// This is equivalent to a typical league season where teams play each other home and away.
    /// </summary>
    public static RoundRobinStrategy Default => new(2);

    /// <summary>
    /// Gets the number of times each pair of teams plays against each other.
    /// </summary>
    public int MatchesPerPair { get; } = matchesPerPair;

    /// <summary>
    /// Gets the array indicating whether to invert home/away teams for each stage in the series.
    /// </summary>
    public bool[] InvertTeamsByStage { get; } = invertTeamsByStage;

    /// <summary>
    /// Generates a complete round-robin schedule for the given teams.
    /// </summary>
    /// <param name="teams">The teams that will participate in the round-robin competition.</param>
    /// <returns>
    /// An enumerable sequence of matchday definitions containing all fixtures for the complete round-robin tournament.
    /// </returns>
    public IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams)
    {
        var result = new List<MatchdayDefinition>();

        // Phase 1: Generate all team pairings using round-robin algorithm
        var groupOfFixtures = teams.RoundRobin().ToList();

        // Phase 2: Apply home/away inversion pattern for even-numbered rounds
        for (var i = 0; i < groupOfFixtures.Count; i++)
        {
            var invert = i % 2 == 0;

            if (!invert)
                continue;

            var matches = groupOfFixtures[i].ToList();
            groupOfFixtures[i] = matches.Select(static x => (x.Item2, x.Item1));
        }

        // Phase 3: Create multiple stages based on MatchesPerPair configuration
        for (var stageIndex = 0; stageIndex < MatchesPerPair; stageIndex++)
        {
            var invertTeams = InvertTeamsByStage.ElementAtOrDefault(stageIndex);

            // Phase 4: Organize fixtures into matchdays for this stage
            foreach (var fixtureList in groupOfFixtures)
            {
                var fixtures = new List<FixtureDefinition>();

                var matches = fixtureList.ToList();

                for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
                {
                    var team1 = matches[matchIndex].Item1;
                    var team2 = matches[matchIndex].Item2;

                    if (!invertTeams)
                        fixtures.Add(new(team1, team2));
                    else
                        fixtures.Add(new(team2, team1));
                }

                result.Add(new(fixtures));
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates the total number of matchdays required for the round-robin competition.
    /// </summary>
    /// <param name="teamCount">The number of teams participating in the competition.</param>
    /// <returns>The total number of matchdays needed to complete all rounds of the competition.</returns>
    public int GetMatchdayCount(int teamCount) => (teamCount % 2 == 0 ? teamCount - 1 : teamCount) * MatchesPerPair;

    /// <summary>
    /// Calculates the maximum number of matches that can be played simultaneously in any matchday.
    /// </summary>
    /// <param name="teamCount">The number of teams participating in the competition.</param>
    /// <returns>The maximum number of concurrent matches possible in a single matchday.</returns>
    public int GetMaxMatchesPerMatchday(int teamCount) => teamCount / 2;
}
