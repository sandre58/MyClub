﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDatesStageScheduler : IDateScheduler<IMatchesStage>
    {
        private List<(DateTime date, IEnumerable<DateTime> datesOfMatches)> _dates = [];

        public ByDatesStageScheduler SetDates(IEnumerable<DateTime> dates, TimeOnly time)
        {
            _dates = dates.Select(x => (x, EnumerableHelper.Range(1, 100, 1).Select(y => x.At(time)))).ToList();
            return this;
        }

        public ByDatesStageScheduler SetDates(IEnumerable<DateTime> dates, IEnumerable<TimeOnly> times)
        {
            _dates = dates.Select(x => (x, times.Select(y => x.At(y)))).ToList();
            return this;
        }

        public ByDatesStageScheduler SetDates(IEnumerable<(DateTime date, IEnumerable<TimeOnly> times)> dates)
        {
            _dates = dates.Select(x => (x.date, x.times.Select(y => x.date.At(y)))).ToList();
            return this;
        }

        public ByDatesStageScheduler SetDates(IEnumerable<(DateTime date, IEnumerable<DateTime> dates)> dates)
        {
            _dates = dates.ToList();
            return this;
        }

        public void Schedule(IEnumerable<IMatchesStage> items)
            => items.ForEach((item, index) =>
            {
                item.Schedule(_dates.GetByIndex(index).date);

                item.GetAllMatches().OrderBy(x => x.Date).ToList().ForEach((match, matchIndex) =>
                {
                    var date = _dates.GetByIndex(index).datesOfMatches?.ToList().GetByIndex(matchIndex, item.Date) ?? item.Date;

                    match.Schedule(date);
                });
            });
        public DateTime GetFromDate() => _dates.LastOrDefault().date;

        public void Reset(DateTime fromDate, IEnumerable<IMatchesStage>? scheduledItems = null) => _dates.Clear();
    }
}

