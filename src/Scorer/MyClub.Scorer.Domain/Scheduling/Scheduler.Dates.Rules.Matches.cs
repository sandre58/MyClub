// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class DateRulesMatchesScheduler : DateRulesScheduler<Match>, IMatchesScheduler
    {
        public bool ScheduleByParent { get; set; }

        public override void Schedule(IEnumerable<Match> items)
        {
            if (!ScheduleByParent)
            {
                var date = StartDate;
                DateOnly? previousDate = null;
                items.ForEach(x =>
                {
                    var matchIndex = x.Parent.Matches.OrderBy(x => x.Date).ToList().IndexOf(x);
                    while (!DateRules.All(y => y.Match(date, previousDate)))
                        date = date.BeginningOfDay().Add(Interval).ToDate();

                    var time = TimeRules.Select(y => y.ProvideTime(date, matchIndex)).FirstOrDefault(y => y is not null) ?? DefaultTime ?? x.Date.ToTime();

                    x.Schedule(date.At(time, DateTimeKind));

                    previousDate = date;
                    date = date.BeginningOfDay().Add(Interval).ToDate();
                });
            }
            else
            {
                var itemsGroupByParents = items.OrderBy(x => x.Date).GroupBy(x => x.Parent);
                var date = StartDate;
                DateOnly? previousDate = null;
                itemsGroupByParents.ForEach(item =>
                {
                    while (!DateRules.All(x => x.Match(date, previousDate)))
                        date = date.BeginningOfDay().Add(Interval).ToDate();

                    item.OrderBy(x => x.Date).ToList().ForEach(match =>
                    {
                        var matchIndex = match.Parent.Matches.OrderBy(x => x.Date).ToList().IndexOf(match);
                        var time = TimeRules.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? match.Date.ToTime();

                        match.Schedule(date.At(time, DateTimeKind));
                    });

                    previousDate = date;
                    date = date.BeginningOfDay().Add(Interval).ToDate();
                });
            }
        }
    }
}

