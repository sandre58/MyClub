// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDayOfWeekScheduler<T> : IScheduler<T> where T : ISchedulable
    {
        public DateOnly StartDate { get; set; } = DateTime.UtcNow.ToDate();

        public TimeOnly Time { get; set; } = new(15, 0, 0);

        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;

        public IEnumerable<DayOfWeek> DayOfWeeks { get; set; } = [DayOfWeek.Sunday];

        public void Schedule(IEnumerable<T> items)
        {
            var previousDate = StartDate.PreviousDay();
            items.ForEach(x =>
            {
                x.Schedule(DayOfWeeks.Min(x => previousDate.Next(x)).At(Time, DateTimeKind));

                if (x is IMatchesProvider matchesProvider)
                    matchesProvider.Matches.ForEach(y => y.Schedule(x.Date));

                previousDate = x.Date.ToDate();
            });
        }
    }
}

