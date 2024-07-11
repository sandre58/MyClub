// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDayOfWeekMatchdaysScheduler : IMatchdaysScheduler
    {
        public static readonly ByDayOfWeekMatchdaysScheduler Default = new();

        public DateTime StartDate { get; set; } = DateTime.Today;

        public TimeSpan Time { get; set; } = new TimeSpan(15, 0, 0);

        public IEnumerable<DayOfWeek> DayOfWeeks { get; set; } = [DayOfWeek.Sunday];

        public void Schedule(IEnumerable<SchedulingMatchdayInformation> matchdays)
        {
            var previousDate = StartDate.PreviousDay().BeginningOfDay();
            matchdays.ForEach(x =>
            {
                x.Matchday.Schedule(DayOfWeeks.Min(x => previousDate.Next(x)).BeginningOfDay().ToUtcDateTime(Time));
                x.Matchday.Matches.ForEach(y => y.Schedule(x.Matchday.Date));
            });
        }
    }
}

