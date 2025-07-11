// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class DateRulesMatchesScheduler : DateRulesScheduler<Match>
    {
        private DateOnly _fromDate;

        public DateRulesMatchesScheduler(DateOnly fromDate) : base(fromDate) => _fromDate = fromDate;

        public bool ScheduleByStage { get; set; }

        public override void Schedule(IEnumerable<Match> items)
        {
            if (!ScheduleByStage)
            {
                DateOnly? previousDate = null;
                items.ForEach(item =>
                {
                    var matchIndex = item.GetStage().GetAllMatches().OrderBy(x => x.Date).ToList().IndexOf(item);
                    var newDate = ScheduleItem(item, _fromDate, previousDate, matchIndex);

                    previousDate = newDate.ToDate();
                    _fromDate = newDate.BeginningOfDay().Add(Interval).ToDate();
                });
            }
            else
            {
                var itemsGroupByStages = items.OrderBy(x => x.Date).GroupBy(x => x.GetStage());
                DateOnly? previousDate = null;
                itemsGroupByStages.ForEach(item =>
                {
                    var newDate = ComputeNextDate(_fromDate, previousDate);

                    item.OrderBy(x => x.Date).ToList().ForEach(match =>
                    {
                        var matchIndex = match.GetStage().GetAllMatches().OrderBy(x => x.Date).ToList().IndexOf(match);
                        var time = ComputeTime(match, newDate, matchIndex);

                        match.Schedule(newDate.At(time, DateTimeKind));
                    });

                    previousDate = newDate;
                    _fromDate = newDate.BeginningOfDay().Add(Interval).ToDate();
                });
            }
        }

        public override DateTime GetFromDate() => _fromDate.BeginningOfDay();

        public override void Reset(DateTime fromDate, IEnumerable<Match>? scheduledItems = null) => _fromDate = fromDate.ToDate();
    }
}

