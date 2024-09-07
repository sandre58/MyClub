// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
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
            var date = StartDate;
            DateOnly? previousDate = null;
            var maxDate = DateTime.MaxValue.Date.Subtract(Interval).ToDate();
            items.ForEach(item =>
            {
                while (!DateRules.All(x => x.Match(date, previousDate)) && date < maxDate)
                    date = date.BeginningOfDay().Add(Interval).ToDate();

                if (date >= maxDate)
                    date = StartDate;

                TimeOnly time;
                if (item is IMatchesProvider matchesProvider && matchesProvider.Matches.Count > 0)
                {
                    matchesProvider.Matches.OrderBy(x => x.Date).ToList().ForEach((match, matchIndex) =>
                    {
                        var time = TimeRules.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? item.Date.ToTime();

                        match.Schedule(date.At(time, DateTimeKind));
                    });

                    time = matchesProvider.Matches.MinOrDefault(x => x.Date.ToTime(), DefaultTime.GetValueOrDefault());
                }
                else
                    time = TimeRules.Select(x => x.ProvideTime(date, 0)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? item.Date.ToTime();

                item.Schedule(date.At(time, DateTimeKind));

                previousDate = date;
                date = date.BeginningOfDay().Add(Interval).ToDate();
            });
        }
    }
}

