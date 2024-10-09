// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class DateRulesScheduler<T> : IScheduler<T> where T : ISchedulable
    {
        public DateOnly StartDate { get; set; } = DateTime.UtcNow.ToDate();

        public TimeOnly? DefaultTime { get; set; }

        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;

        public TimeSpan Interval { get; set; } = 1.Days();

        public List<IDateSchedulingRule> DateRules { get; set; } = [];

        public List<ITimeSchedulingRule> TimeRules { get; set; } = [];

        public virtual void Schedule(IEnumerable<T> items)
        {
            var startDate = StartDate;
            DateOnly? previousDate = null;
            items.ForEach((item, index) =>
            {
                var newDate = ScheduleItem(item, startDate, previousDate, index);

                previousDate = newDate.ToDate();
                startDate = newDate.BeginningOfDay().Add(Interval).ToDate();
            });
        }

        protected virtual DateTime ScheduleItem(T item, DateOnly startDate, DateOnly? previousDate, int index)
        {
            var date = ComputeNextDate(startDate, previousDate);

            var time = ComputeTime(item, date, index);

            item.Schedule(date.At(time, DateTimeKind));

            return item.Date;
        }

        protected virtual DateOnly ComputeNextDate(DateOnly startDate, DateOnly? previousDate)
        {
            var date = startDate;
            var maxDate = DateTime.MaxValue.Date.Subtract(Interval).ToDate();

            while (!DateRules.All(x => x.Match(date, previousDate)) && date < maxDate)
                date = date.BeginningOfDay().Add(Interval).ToDate();

            if (date >= maxDate)
                date = startDate;

            return date;
        }

        protected virtual TimeOnly ComputeTime(T item, DateOnly date, int index) => TimeRules.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? item.Date.ToTime();
    }
}

