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
        public DateTime StartDate { get; set; } = DateTime.Now;

        public TimeSpan? DefaultTime { get; set; }

        public TimeSpan Interval { get; set; } = 1.Days();

        public List<IDateSchedulingRule> DateRules { get; set; } = [];

        public List<ITimeSchedulingRule> TimeRules { get; set; } = [];

        public virtual void Schedule(IEnumerable<T> items)
        {
            var date = StartDate;
            DateTime? previousDate = null;
            var maxDate = DateTime.MaxValue.Date.Subtract(Interval);
            items.ForEach(item =>
            {
                while (!DateRules.All(x => x.Match(date, previousDate)) && date < maxDate)
                    date = date.Date.Add(Interval);

                if (date >= maxDate)
                    date = StartDate;

                var itemDate = date.ToUtcDateTime(DefaultTime ?? item.Date.ToLocalTime().TimeOfDay);
                if (item is IMatchesProvider matchesProvider)
                {
                    matchesProvider.Matches.OrderBy(x => x.Date).ToList().ForEach((match, matchIndex) =>
                    {
                        var time = TimeRules.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? item.Date.ToLocalTime().TimeOfDay;

                        match.Schedule(date.ToUtcDateTime(time));
                    });

                    itemDate = date.ToUtcDateTime(matchesProvider.Matches.MinOrDefault(x => x.Date.ToLocalTime().TimeOfDay));
                }

                item.Schedule(itemDate);

                previousDate = date.Date;
                date = date.Date.Add(Interval);
            });
        }
    }
}

