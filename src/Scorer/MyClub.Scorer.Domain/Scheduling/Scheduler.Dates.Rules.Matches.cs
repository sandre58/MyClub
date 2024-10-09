// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class DateRulesMatchesScheduler : DateRulesScheduler<Match>, IMatchesScheduler
    {
        public bool ScheduleByStage { get; set; }

        public override void Schedule(IEnumerable<Match> items)
        {
            var startDate = StartDate;
            if (!ScheduleByStage)
            {
                DateOnly? previousDate = null;
                items.ForEach(item =>
                {
                    var matchIndex = item.GetStage().GetAllMatches().OrderBy(x => x.Date).ToList().IndexOf(item);
                    var newDate = ScheduleItem(item, startDate, previousDate, matchIndex);

                    previousDate = newDate.ToDate();
                    startDate = newDate.BeginningOfDay().Add(Interval).ToDate();
                });
            }
            else
            {
                var itemsGroupByStages = items.OrderBy(x => x.Date).GroupBy(x => x.GetStage());
                DateOnly? previousDate = null;
                itemsGroupByStages.ForEach(item =>
                {
                    var newDate = ComputeNextDate(startDate, previousDate);

                    item.OrderBy(x => x.Date).ToList().ForEach(match =>
                    {
                        var matchIndex = match.GetStage().GetAllMatches().OrderBy(x => x.Date).ToList().IndexOf(match);
                        var time = ComputeTime(match, newDate, matchIndex);

                        match.Schedule(newDate.At(time, DateTimeKind));
                    });

                    previousDate = newDate;
                    startDate = newDate.BeginningOfDay().Add(Interval).ToDate();
                });
            }
        }
    }
}

