// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDayOfWeekStageScheduler : ByDayOfWeekScheduler<IMatchesStage>
    {
        public ByDayOfWeekStageScheduler(DateOnly fromDate) : base(fromDate)
        {
        }

        protected override DateTime ScheduleItem(IMatchesStage item, DateOnly previousDate, TimeOnly time)
        {
            var newDate = base.ScheduleItem(item, previousDate, time);

            item.GetAllMatches().ForEach(y => y.Schedule(newDate));

            return newDate;
        }
    }
}

