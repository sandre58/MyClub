// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDatesMatchdaysScheduler : IMatchdaysScheduler
    {
        public static readonly ByDatesMatchdaysScheduler Default = new();

        private IList<(DateTime date, IEnumerable<DateTime> datesOfMatches)> _dates = [];

        public ByDatesMatchdaysScheduler SetDates(IEnumerable<DateTime> dates, TimeSpan time)
        {
            _dates = dates.Select(x => (x, EnumerableHelper.Range(1, 100, 1).Select(y => x.ToUtcDateTime(time)))).ToList();
            return this;
        }

        public ByDatesMatchdaysScheduler SetDates(IEnumerable<DateTime> dates, IEnumerable<TimeSpan> times)
        {
            _dates = dates.Select(x => (x, times.Select(y => x.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public ByDatesMatchdaysScheduler SetDates(IEnumerable<(DateTime date, IEnumerable<TimeSpan> times)> dates)
        {
            _dates = dates.Select(x => (x.date, x.times.Select(y => x.date.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public ByDatesMatchdaysScheduler SetDates(IEnumerable<(DateTime date, IEnumerable<DateTime> dates)> dates)
        {
            _dates = dates.ToList();
            return this;
        }

        public void Schedule(IEnumerable<SchedulingMatchdayInformation> matchdays)
        {
            foreach (var item in matchdays)
            {
                item.Matchday.Schedule(_dates.GetByIndex(item.Index).date.ToUniversalTime());

                item.Matchday.Matches.ForEach((x, y) =>
                {
                    var date = _dates.GetByIndex(y).datesOfMatches?.ToList().GetByIndex(y, item.Matchday.Date).ToUniversalTime() ?? item.Matchday.Date;

                    x.Schedule(date);
                });
            }
        }
    }
}

