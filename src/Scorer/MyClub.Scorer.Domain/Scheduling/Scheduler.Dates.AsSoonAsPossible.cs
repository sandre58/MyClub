﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class AsSoonAsPossibleScheduler<T> : IScheduler<T> where T : ISchedulable, IMatchesProvider
    {
        private readonly IEnumerable<T> _scheduledItems;

        public AsSoonAsPossibleScheduler(IEnumerable<T>? scheduledItems = null) => _scheduledItems = scheduledItems ?? [];

        public DateTime StartDate { get; set; } = DateTime.Now;

        public bool ScheduleVenues { get; set; } = true;

        public List<IAvailableDateSchedulingRule> Rules { get; set; } = [];

        public ICollection<Stadium> AvailableStadiums { get; set; } = [];

        public virtual void Schedule(IEnumerable<T> items)
        {
            var scheduledMatches = new List<Match>(_scheduledItems.Except(items).SelectMany(x => x.Matches));
            items.ForEach(x =>
            {
                var scheduler = new AsSoonAsPossibleMatchesScheduler(scheduledMatches) { StartDate = StartDate, ScheduleVenues = ScheduleVenues, AvailableStadiums = AvailableStadiums, Rules = Rules };
                scheduler.Schedule(x.Matches);

                scheduledMatches.AddRange(x.Matches);
                var dateOfMatchday = x.Matches.MinOrDefault(x => x.Date, x.Date);

                x.Schedule(dateOfMatchday);
            });
        }
    }
}

