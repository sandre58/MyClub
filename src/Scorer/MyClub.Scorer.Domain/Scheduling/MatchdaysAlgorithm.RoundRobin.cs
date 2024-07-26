// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class RoundRobinAlgorithm : IMatchdaysAlgorithm
    {
        public static RoundRobinAlgorithm Default => new();

        public int NumberOfMatchesBetweenTeams { get; set; } = 2;

        public bool[] InvertTeamsByStage { get; set; } = [false, true];

        public int NumberOfMatchdays(int teamsCount) => (teamsCount % 2 == 0 ? teamsCount - 1 : teamsCount) * NumberOfMatchesBetweenTeams;

        public int NumberOfMatchesByMatchday(int teamsCount) => teamsCount / 2;

        public IEnumerable<IEnumerable<(ITeam home, ITeam away)>> Build(IEnumerable<ITeam> teams)
        {
            var result = new List<IEnumerable<(ITeam home, ITeam away)>>();

            var matchdays = teams.RoundRobin().ToList();
            for (var i = 0; i < matchdays.Count; i++)
            {
                var invert = i % 2 == 0;

                if (invert)
                {
                    var matches = matchdays[i].ToList();
                    matchdays[i] = matches.Select(x => (x.item2, x.item1));
                }
            }

            for (var stageIndex = 0; stageIndex < NumberOfMatchesBetweenTeams; stageIndex++)
            {
                var invertTeams = InvertTeamsByStage.ElementAtOrDefault(stageIndex);
                for (var matchdayIndex = 0; matchdayIndex < matchdays.Count; matchdayIndex++)
                {
                    var matchday = new List<(ITeam home, ITeam away)>();

                    var matches = matchdays[matchdayIndex].ToList();

                    for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
                    {
                        var team1 = matches[matchIndex].item1;
                        var team2 = matches[matchIndex].item2;

                        if (!invertTeams)
                            matchday.Add((team1, team2));
                        else
                            matchday.Add((team2, team1));
                    }

                    result.Add(matchday);
                }
            }

            return result;
        }
    }
}

