// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDayOfWeekStageScheduler<T> : ByDayOfWeekScheduler<T> where T : ISchedulable, IMatchesProvider
    {
        protected override DateTime ScheduleItem(T item, DateOnly previousDate, TimeOnly time)
        {
            var newDate = base.ScheduleItem(item, previousDate, time);

            item.GetAllMatches().ForEach(y => y.Schedule(newDate));

            return newDate;
        }
    }
}

