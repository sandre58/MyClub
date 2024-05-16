// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;

namespace MyClub.Teamup.Domain.Factories.Extensions
{
    public static class LeagueExtensions
    {
        public static IEnumerable<Matchday> ComputeMatchdays(this GroupStage groupStage, MatchdaysBuilder matchdaysBuilder)
        {
            var matchdaysByGroup = new Dictionary<Group, List<Matchday>>();
            foreach (var group in groupStage.Groups)
            {
                var matchdaysForGroup = matchdaysBuilder.Build(group.Teams, groupStage.Rules.MatchFormat).ToList();

                if (matchdaysForGroup.Count > 0)
                    matchdaysByGroup.Add(group, matchdaysForGroup);
            }

            var matchdays = new List<Matchday>();
            var dateOfCurrentMatchday = matchdaysBuilder.StartDate.PreviousDay().BeginningOfDay();

            for (var i = 1; i <= matchdaysByGroup.MaxOrDefault(x => x.Value.Count); i++)
            {
                dateOfCurrentMatchday = ComputeMatchdayDate(dateOfCurrentMatchday, [DayOfWeek.Sunday]);
                var matchday = new Matchday(matchdaysBuilder.ProvideName(i), dateOfCurrentMatchday.AddFluentTimeSpan(matchdaysBuilder.Time), matchdaysBuilder.ProvideShortName(i), groupStage.Rules.MatchFormat);
                matchdays.Add(matchday);
            }

            foreach (var group in matchdaysByGroup.Select(x => x.Value))
                for (var i = 0; i < group.Count; i++)
                    group[i].Matches.ForEach(x => matchdays[i].AddMatch(x));

            groupStage.ClearMatchdays();
            return matchdays.Select(groupStage.AddMatchday).ToList();

        }

        public static IEnumerable<Matchday> ComputeMatchdays(this IHasMatchdays championship, MatchdaysBuilder matchdaysBuilder)
        {
            var matchdays = matchdaysBuilder.Build(championship.Teams, championship.Rules.MatchFormat).ToList();

            championship.ClearMatchdays();
            return matchdays.Select(championship.AddMatchday).ToList();
        }

        public static void FillMatchdays(this IHasMatchdays championship)
        {
            var matchdays = championship.Matchdays.OrderBy(x => x.OriginDate).ToList();
            var rounds = championship.Teams.RoundRobin().ToList();
            var numberOfFixturesBetwwenTeams = Math.Round(matchdays.Count / (double)rounds.Count);

            for (var stageIndex = 0; stageIndex < numberOfFixturesBetwwenTeams; stageIndex++)
            {
                for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
                {
                    var matchdayIndex = stageIndex * rounds.Count + roundIndex;

                    if (matchdays.Count >= matchdayIndex) return;

                    var matchday = matchdays[matchdayIndex];

                    var list = rounds[roundIndex].ToList();

                    for (var matchIndex = 0; matchIndex < list.Count; matchIndex++)
                    {
                        var team1 = list[matchIndex].item1;
                        var team2 = list[matchIndex].item2;

                        var match = matchday.AddMatch(team1, team2);

                        if (roundIndex % 2 != stageIndex % 2) match.Invert();
                    }
                }
            }
        }

        private static DateTime ComputeMatchdayDate(DateTime start, DayOfWeek[] dayOfWeeks) => dayOfWeeks.Min(x => start.Next(x)).BeginningOfDay();

    }
}
