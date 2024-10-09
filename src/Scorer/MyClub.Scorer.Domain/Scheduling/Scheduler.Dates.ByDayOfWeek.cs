// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
                var newDate = ScheduleItem(x, previousDate, Time);

                previousDate = newDate.ToDate();
            });
        }

        protected virtual DateTime ScheduleItem(T item, DateOnly previousDate, TimeOnly time)
        {
            var date = DayOfWeeks.Min(x => previousDate.Next(x)).At(time, DateTimeKind);
            item.Schedule(date);

            return item.Date;
        }
    }
}

