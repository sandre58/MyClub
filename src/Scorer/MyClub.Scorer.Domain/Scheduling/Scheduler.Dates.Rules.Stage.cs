// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class DateRulesStageScheduler<T> : DateRulesScheduler<T> where T : ISchedulable, IMatchesProvider
    {
        protected override TimeOnly ComputeTime(T item, DateOnly date, int index)
        {
            TimeOnly time;
            var matches = item.GetAllMatches().OrderBy(x => x.Date).ToList();
            if (matches.Count > 0)
            {
                matches.ForEach((match, matchIndex) =>
                    {
                        var time = TimeRules.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) ?? DefaultTime ?? item.Date.ToTime();

                        match.Schedule(date.At(time, DateTimeKind));
                    });
                time = matches.MinOrDefault(x => x.Date.ToTime(), DefaultTime.GetValueOrDefault());
            }
            else
                time = base.ComputeTime(item, date, -1);

            return time;
        }
    }
}
