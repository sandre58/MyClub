// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class MatchdaysBuilder : IMatchdaysBuilder
    {
        private readonly IScheduler<Matchday> _scheduler;
        private readonly IMatchesScheduler? _venuesScheduler;

        public MatchdaysBuilder(IScheduler<Matchday> scheduler, IMatchesScheduler? venuesScheduler = null)
        {
            _scheduler = scheduler;
            _venuesScheduler = venuesScheduler;
        }

        public string NamePattern { get; set; } = MyClubResources.MatchdayNamePattern;

        public string ShortNamePattern { get; set; } = MyClubResources.MatchdayShortNamePattern;

        public bool ScheduleVenuesBeforeDates { get; set; } = false;

        public virtual IEnumerable<Matchday> Build(IMatchdaysStage stage, IMatchdaysAlgorithm algorithm)
        {
            // 1 - Create matchdays and matches with default dates and names
            var matchdays = algorithm.Compute(stage.ProvideTeams())
                                     .Select((x, y) =>
                                     {
                                         var matchday = new Matchday(stage, DateTime.Today, (y + 1).ToString(MyClubResources.MatchdayX), (y + 1).ToString(MyClubResources.MatchdayXAbbr));
                                         x.ForEach(x => matchday.AddMatch(x.Team1, x.Team2));

                                         return matchday;
                                     })
                                     .ToList();

            // Schedule matchdays
            void scheduleVenues() => _venuesScheduler?.Schedule(matchdays.SelectMany(x => x.Matches).ToList());
            ScheduleVenuesBeforeDates.IfTrue(scheduleVenues);
            _scheduler.Schedule(matchdays);
            ScheduleVenuesBeforeDates.IfFalse(scheduleVenues);

            // Update names
            matchdays.OrderBy(x => x.Date).ForEach((x, y) =>
            {
                x.Name = StageNamesFactory.ComputePattern(NamePattern, y + 1, x);
                x.ShortName = StageNamesFactory.ComputePattern(ShortNamePattern, y + 1, x);
            });

            return matchdays;
        }
    }
}

