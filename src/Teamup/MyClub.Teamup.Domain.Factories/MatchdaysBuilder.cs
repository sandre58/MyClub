// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.Factories
{
    public class MatchdaysBuilder
    {
        public Func<int, string> ProvideName { get; set; } = new Func<int, string>(x => $"{MyClubResources.Matchday} {x}");

        public Func<int, string> ProvideShortName { get; set; } = new Func<int, string>(x => $"{MyClubResources.Matchday.Substring(0, 1)}{x}");

        public int NumberOfFixturesBetwwenTeams { get; set; } = 2;

        public DateTime StartDate { get; set; } = DateTime.Today;

        public TimeSpan Time { get; set; } = new TimeSpan(15, 0, 0);

        public IEnumerable<DayOfWeek> DayOfWeeks { get; set; } = [DayOfWeek.Sunday];

        public IEnumerable<Matchday> Build(IEnumerable<Team> teams, MatchFormat? matchFormat = null)
        {
            var matchdays = new List<Matchday>();

            var rounds = teams.RoundRobin().ToList();

            var dateOfCurrentMatchday = StartDate.PreviousDay().BeginningOfDay().ToUniversalTime();

            for (var stageIndex = 0; stageIndex < NumberOfFixturesBetwwenTeams; stageIndex++)
            {
                for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
                {
                    var matchdayNumber = stageIndex * rounds.Count + roundIndex + 1;
                    dateOfCurrentMatchday = ComputeMatchdayDate(dateOfCurrentMatchday, DayOfWeeks.ToArray());
                    var matchday = new Matchday(ProvideName(matchdayNumber), dateOfCurrentMatchday.AddFluentTimeSpan(Time), ProvideShortName(matchdayNumber), matchFormat);

                    var list = rounds[roundIndex].ToList();

                    for (var matchIndex = 0; matchIndex < list.Count; matchIndex++)
                    {
                        var team1 = list[matchIndex].item1;
                        var team2 = list[matchIndex].item2;

                        var match = matchday.AddMatch(team1, team2);

                        if (roundIndex % 2 != stageIndex % 2) match.Invert();
                    }

                    matchdays.Add(matchday);
                }
            }

            return matchdays;
        }

        private static DateTime ComputeMatchdayDate(DateTime start, DayOfWeek[] dayOfWeeks) => dayOfWeeks.Min(x => start.Next(x)).BeginningOfDay();
    }
}
