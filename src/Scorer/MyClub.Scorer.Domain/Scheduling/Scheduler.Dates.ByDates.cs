// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDatesScheduler<T> : IScheduler<T> where T : ISchedulable
    {
        private IList<(DateTime date, IEnumerable<DateTime> datesOfMatches)> _dates = [];

        public ByDatesScheduler<T> SetDates(IEnumerable<DateTime> dates, TimeSpan time)
        {
            _dates = dates.Select(x => (x, EnumerableHelper.Range(1, 100, 1).Select(y => x.ToUtcDateTime(time)))).ToList();
            return this;
        }

        public ByDatesScheduler<T> SetDates(IEnumerable<DateTime> dates, IEnumerable<TimeSpan> times)
        {
            _dates = dates.Select(x => (x, times.Select(y => x.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public ByDatesScheduler<T> SetDates(IEnumerable<(DateTime date, IEnumerable<TimeSpan> times)> dates)
        {
            _dates = dates.Select(x => (x.date, x.times.Select(y => x.date.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public ByDatesScheduler<T> SetDates(IEnumerable<(DateTime date, IEnumerable<DateTime> dates)> dates)
        {
            _dates = dates.ToList();
            return this;
        }

        public void Schedule(IEnumerable<T> items)
        => items.ForEach((item, index) =>
            {
                item.Schedule(_dates.GetByIndex(index).date.ToUniversalTime());

                if (item is IMatchesProvider matchesProvider)
                {
                    matchesProvider.Matches.OrderBy(x => x.Date).ToList().ForEach((match, matchIndex) =>
                    {
                        var date = _dates.GetByIndex(index).datesOfMatches?.ToList().GetByIndex(matchIndex, item.Date).ToUniversalTime() ?? item.Date;

                        match.Schedule(date);
                    });
                }
            });
    }
}

