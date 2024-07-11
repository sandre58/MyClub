// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class SwissSystemAlgorithm : IMatchdaysAlgorithm
    {
        public static SwissSystemAlgorithm Default => new();

        public int NumberOfMatchesByTeams { get; set; } = 2;

        public int NumberOfMatchdays(int teamsCount) => NumberOfMatchesByTeams;

        public int NumberOfMatchesByMatchday(int teamsCount) => teamsCount / 2;

        public IEnumerable<IEnumerable<(ITeam home, ITeam away)>> Build(IEnumerable<ITeam> teams)
        {
            if (teams.Count() % 2 != 0) throw new InvalidOperationException("Swiss System works only with odd number of teams.");

            var result = new List<IEnumerable<(ITeam home, ITeam away)>>();

            var matchdays = teams.RoundRobin().ToList();
            var numberOfMatchesByTeams = Math.Min(matchdays.Count, NumberOfMatchesByTeams);

            for (var i = 0; i < matchdays.Count; i++)
            {
                var invert = i % 2 == 0;

                if (invert)
                {
                    var matches = matchdays[i].ToList();
                    matchdays[i] = matches.Select(x => (x.item2, x.item1));
                }
            }

            for (var matchdayIndex = 0; matchdayIndex < numberOfMatchesByTeams; matchdayIndex++)
            {
                var matchday = new List<(ITeam home, ITeam away)>();

                var matches = matchdays[matchdayIndex].ToList();

                foreach (var (home, away) in matches)
                    matchday.Add((home, away));

                result.Add(matchday);
            }

            return result;
        }
    }
}

