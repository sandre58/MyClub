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

public class RoundRobinStrategy(int matchesPerPair, bool[] invertTeamsByStage) : IMatchdayScheduleStrategy
{
    public RoundRobinStrategy(int matchesPerPair)
        : this(matchesPerPair, [.. Enumerable.Range(1, matchesPerPair).Select(x => x % 2 == 0)]) { }

    public static RoundRobinStrategy Default => new(2);

    public int MatchesPerPair { get; private set; } = matchesPerPair;

    public bool[] InvertTeamsByStage { get; private set; } = invertTeamsByStage;

    public IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams)
    {
        var result = new List<MatchdayDefinition>();

        var groupOfFixtures = teams.RoundRobin().ToList();
        for (var i = 0; i < groupOfFixtures.Count; i++)
        {
            var invert = i % 2 == 0;

            if (invert)
            {
                var matches = groupOfFixtures[i].ToList();
                groupOfFixtures[i] = matches.Select(x => (x.Item2, x.Item1));
            }
        }

        for (var stageIndex = 0; stageIndex < MatchesPerPair; stageIndex++)
        {
            var invertTeams = InvertTeamsByStage.ElementAtOrDefault(stageIndex);
            for (var matchdayIndex = 0; matchdayIndex < groupOfFixtures.Count; matchdayIndex++)
            {
                var fixtures = new List<FixtureDefinition>();

                var matches = groupOfFixtures[matchdayIndex].ToList();

                for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
                {
                    var team1 = matches[matchIndex].Item1;
                    var team2 = matches[matchIndex].Item2;

                    if (!invertTeams)
                        fixtures.Add(new(team1, team2));
                    else
                        fixtures.Add(new(team2, team1));
                }

                result.Add(new MatchdayDefinition(fixtures));
            }
        }

        return result;
    }

    public int GetMatchdayCount(int teamCount) => (teamCount % 2 == 0 ? teamCount - 1 : teamCount) * MatchesPerPair;

    public int GetMaxMatchesPerMatchday(int teamCount) => teamCount / 2;
}
