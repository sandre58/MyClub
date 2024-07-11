// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class MatchdaysBuilder
    {
        public string NamePattern { get; set; } = MyClubResources.MatchdayNamePattern;

        public string ShortNamePattern { get; set; } = MyClubResources.MatchdayShortNamePattern;

        public IMatchdaysScheduler Scheduler { get; set; }

        public virtual IEnumerable<Matchday> Build(IMatchdaysProvider matchdaysProvider, IMatchdaysAlgorithm algorithm)
        {
            var result = new List<Matchday>();

            var matchdays = algorithm.Build(matchdaysProvider.Teams)
                                     .Select((x, y) => new { Index = y, Matches = x })
                                     .ToDictionary(x => x.Index, x => x.Matches.Select((x, y) => new { Index = y, HomeTeam = x.home, AwayTeam = x.away }))
                                     .ToList();

            // 1 - Create matchdays and matches with default dates and names
            foreach (var item in matchdays)
            {
                var matchdayNumber = item.Key + 1;
                var matchday = new Matchday(matchdaysProvider, DateTime.Today, $"{MyClubResources.Matchday} {matchdayNumber}", matchdayNumber.ToString(MyClubResources.MatchdayXAbbr));
                item.Value.ForEach(x => matchday.AddMatch(x.HomeTeam, x.AwayTeam));

                result.Add(matchday);
            }

            // 2 - Schedule matchdays
            var schedulingMatchdaysInformations = result.Select((x, y) => new SchedulingMatchdayInformation(x, y)).ToList();
            Scheduler.Schedule(schedulingMatchdaysInformations);

            // 3 - Update names
            result.OrderBy(x => x.Date).ForEach((x, y) =>
            {
                x.Name = StageNamesFactory.ComputePattern(NamePattern, y + 1, x.Date);
                x.ShortName = StageNamesFactory.ComputePattern(ShortNamePattern, y + 1, x.Date);
            });

            return result;
        }
    }
}

