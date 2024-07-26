// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Exceptions;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class IncludeDaysOfWeekRule : ValueObject, IDateSchedulingRule, IAvailableDateSchedulingRule
    {
        public IncludeDaysOfWeekRule(ICollection<DayOfWeek> daysOfWeek) => DaysOfWeek = daysOfWeek;

        public ICollection<DayOfWeek> DaysOfWeek { get; }

        public bool Match(DateTime date, DateTime? previousDate) => DaysOfWeek.Contains(date.DayOfWeek);

        public IEnumerable<Period> GetAvailablePeriods(Period period)
            => period.ByDays().Where(x => DaysOfWeek.Contains(x.Start.DayOfWeek));

        public DateTime GetNextAvailableDate(Period matchPeriod)
            => DaysOfWeek.Count == 0 || DaysOfWeek.Contains(matchPeriod.Start.DayOfWeek) ? matchPeriod.Start : NextDay(matchPeriod.Start.Date);


        private DateTime NextDay(DateTime start)
        {
            do
            {
                start = start.NextDay();
            } while (!DaysOfWeek.Contains(start.DayOfWeek));

            return start;
        }
    }

    public class ExcludeDateRule : ValueObject, IDateSchedulingRule
    {
        public ExcludeDateRule(DateTime date) => Date = date;

        public DateTime Date { get; }

        public bool Match(DateTime date, DateTime? previousDate) => date.Date != Date.Date;
    }

    public class ExcludeDatesRangeRule : ValueObject, IDateSchedulingRule, IAvailableDateSchedulingRule
    {
        public ExcludeDatesRangeRule(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        private ImmutablePeriod GetPeriod() => new Period(StartDate, EndDate).AsImmutable();

        public IEnumerable<Period> GetAvailablePeriods(Period period) => period.Exclude(new Period(StartDate, EndDate));

        public bool Match(DateTime date, DateTime? previousDate) => !GetPeriod().Contains(date);

        public DateTime GetNextAvailableDate(Period matchPeriod) => GetPeriod().IntersectWith(matchPeriod) ? EndDate.NextDay().BeginningOfDay() : matchPeriod.Start;
    }

    public class DateIntervalRule : ValueObject, IDateSchedulingRule
    {
        public DateIntervalRule(TimeSpan interval) => Interval = interval;

        public TimeSpan Interval { get; }

        public bool Match(DateTime date, DateTime? previousDate) => !previousDate.HasValue || date >= previousDate.Value.Add(Interval);
    }

    public class IncludeTimePeriodsRule : ValueObject, IAvailableDateSchedulingRule
    {
        public IncludeTimePeriodsRule(IEnumerable<TimePeriod> periods) => Periods = new List<TimePeriod>(periods).AsReadOnly();

        public IReadOnlyCollection<TimePeriod> Periods { get; }

        public IEnumerable<Period> GetAvailablePeriods(Period period) => Periods.SelectMany(period.Intersect);

        public DateTime GetNextAvailableDate(Period matchPeriod)
            => Periods.MinOrDefault(x => GetNextAvailableDate(x, matchPeriod), matchPeriod.Start);

        private static DateTime GetNextAvailableDate(TimePeriod utcPeriod, Period matchPeriod)
        {
            var matchDate = matchPeriod.Start.Date;
            var matchStartTime = matchPeriod.Start.TimeOfDay;
            var matchEndTime = matchPeriod.End.TimeOfDay;
            var isInPeriod = matchEndTime > matchStartTime && matchDate.ToUtcDateTime(utcPeriod.Start).ToPeriod(matchDate.ToUtcDateTime(utcPeriod.End)).Contains(matchPeriod);

            if (isInPeriod)
                return matchPeriod.Start;

            if (utcPeriod.End - utcPeriod.Start < matchPeriod.Duration)
                throw new TranslatableException("DefinedPeriodIsLowerThanMatchPeriodError");

            if (matchEndTime > utcPeriod.Start)
                matchDate = matchDate.AddDays(1);

            return matchDate.ToUtcDateTime(utcPeriod.Start);
        }
    }
}

