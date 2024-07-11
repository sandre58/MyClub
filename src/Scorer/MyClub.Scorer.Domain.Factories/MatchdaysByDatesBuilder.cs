// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Factories
{
    public class MatchdaysByDatesBuilder : MatchdaysBuilder
    {
        private IList<(DateTime date, IEnumerable<DateTime> datesOfMatches)> _dates = [];

        public MatchdaysByDatesBuilder SetDates(IEnumerable<DateTime> dates, TimeSpan time)
        {
            _dates = dates.Select(x => (x, EnumerableHelper.Range(1, 100, 1).Select(y => x.ToUtcDateTime(time)))).ToList();
            return this;
        }

        public MatchdaysByDatesBuilder SetDates(IEnumerable<DateTime> dates, IEnumerable<TimeSpan> times)
        {
            _dates = dates.Select(x => (x, times.Select(y => x.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public MatchdaysByDatesBuilder SetDates(IEnumerable<(DateTime date, IEnumerable<TimeSpan> times)> dates)
        {
            _dates = dates.Select(x => (x.date, x.times.Select(y => x.date.ToUtcDateTime(y)))).ToList();
            return this;
        }

        public MatchdaysByDatesBuilder SetDates(IEnumerable<(DateTime date, IEnumerable<DateTime> dates)> dates)
        {
            _dates = dates.ToList();
            return this;
        }

        protected override DateTime GetStartDate() => _dates.FirstOrDefault().date;

        protected override DateTime? ComputeMatchdayDate(DateTime? previousDate, int matchdayIndex) => _dates.GetByIndex(matchdayIndex).date.ToUniversalTime();

        protected override DateTime ComputeMatchDate(DateTime previousDate, MatchInformation currentMatch, IEnumerable<MatchInformation> allMatches)
            => _dates.GetByIndex(currentMatch.MatchIndex).datesOfMatches?.ToList().GetByIndex(currentMatch.MatchIndex, currentMatch.MatchdayDate.GetValueOrDefault()).ToUniversalTime() ?? currentMatch.MatchdayDate.GetValueOrDefault().ToUniversalTime();
    }
}

