// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDayOfWeekScheduler<T> : IDateScheduler<T> where T : ISchedulable
    {
        private DateOnly _fromDate;

        public ByDayOfWeekScheduler(DateOnly fromDate) => _fromDate = fromDate.PreviousDay();

        public TimeOnly Time { get; set; } = new(15, 0, 0);

        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;

        public IEnumerable<DayOfWeek> DayOfWeeks { get; set; } = [DayOfWeek.Sunday];

        public void Schedule(IEnumerable<T> items) => items.ForEach(x => _fromDate = ScheduleItem(x, _fromDate, Time).ToDate());

        protected virtual DateTime ScheduleItem(T item, DateOnly previousDate, TimeOnly time)
        {
            var date = DayOfWeeks.Min(x => previousDate.Next(x)).At(time, DateTimeKind);
            item.Schedule(date);

            return item.Date;
        }

        public DateTime GetFromDate() => _fromDate.BeginningOfDay();

        public void Reset(DateTime fromDate, IEnumerable<T>? scheduledItems = null) => _fromDate = fromDate.PreviousDay().ToDate();
    }
}

