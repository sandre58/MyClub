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

        public bool Match(DateOnly date, DateOnly? previousDate) => DaysOfWeek.Contains(date.DayOfWeek);

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

        public IEnumerable<IncludeDaysOfWeekRule> ConvertToTimeZone() => [new(DaysOfWeek)];

        IEnumerable<IDateSchedulingRule> IDateSchedulingRule.ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => ConvertToTimeZone();

        IEnumerable<IAvailableDateSchedulingRule> IAvailableDateSchedulingRule.ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => ConvertToTimeZone();
    }

    public class ExcludeDateRule : ValueObject, IDateSchedulingRule
    {
        public ExcludeDateRule(DateOnly date) => Date = date;

        public DateOnly Date { get; }

        public bool Match(DateOnly date, DateOnly? previousDate) => date != Date;

        public IEnumerable<IDateSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => [new ExcludeDateRule(Date)];
    }

    public class ExcludeDatesRangeRule : ValueObject, IDateSchedulingRule, IAvailableDateSchedulingRule
    {
        public ExcludeDatesRangeRule(DateOnly startDate, DateOnly endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public DateOnly StartDate { get; }

        public DateOnly EndDate { get; }

        private ImmutablePeriod GetPeriod() => new Period(StartDate.BeginningOfDay(), EndDate.EndOfDay()).AsImmutable();

        public IEnumerable<Period> GetAvailablePeriods(Period period) => period.Exclude(new Period(StartDate.BeginningOfDay(), EndDate.EndOfDay()));

        public bool Match(DateOnly date, DateOnly? previousDate) => !GetPeriod().Contains(date.BeginningOfDay());

        public DateTime GetNextAvailableDate(Period matchPeriod) => GetPeriod().IntersectWith(matchPeriod) ? EndDate.NextDay().BeginningOfDay() : matchPeriod.Start;

        public IEnumerable<ExcludeDatesRangeRule> ConvertToTimeZone() => [new(StartDate, EndDate)];

        IEnumerable<IDateSchedulingRule> IDateSchedulingRule.ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => ConvertToTimeZone();

        IEnumerable<IAvailableDateSchedulingRule> IAvailableDateSchedulingRule.ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => ConvertToTimeZone();
    }

    public class DateIntervalRule : ValueObject, IDateSchedulingRule
    {
        public DateIntervalRule(TimeSpan interval) => Interval = interval;

        public TimeSpan Interval { get; }

        public bool Match(DateOnly date, DateOnly? previousDate) => !previousDate.HasValue || date >= previousDate.Value.BeginningOfDay().Add(Interval).ToDate();

        public IEnumerable<IDateSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => [new DateIntervalRule(Interval)];
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
            var matchStartTime = matchPeriod.Start.ToTime();
            var matchEndTime = matchPeriod.End.ToTime();
            var isInPeriod = matchEndTime > matchStartTime && matchDate.At(utcPeriod.Start).ToPeriod(matchDate.At(utcPeriod.End)).Contains(matchPeriod);

            if (isInPeriod)
                return matchPeriod.Start;

            if (utcPeriod.End - utcPeriod.Start < matchPeriod.Duration)
                throw new TranslatableException("DefinedPeriodIsLowerThanMatchPeriodError");

            if (matchEndTime > utcPeriod.Start)
                matchDate = matchDate.AddDays(1);

            return matchDate.At(utcPeriod.Start);
        }

        public IEnumerable<IAvailableDateSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
        {
            var timePeriods = new List<TimePeriod>();

            foreach (var period in Periods)
            {
                var startTime = period.Start.ToTimeZone(sourceTimeZone, destinationTimeZone);
                var endTime = period.End.ToTimeZone(sourceTimeZone, destinationTimeZone);

                if (endTime < startTime)
                {
                    timePeriods.Add(new TimePeriod(startTime, TimeOnly.MaxValue));

                    if (endTime != TimeOnly.MinValue)
                        timePeriods.Add(new TimePeriod(TimeOnly.MinValue, endTime));
                }
                else
                    timePeriods.Add(new TimePeriod(startTime, endTime));
            }

            return [new IncludeTimePeriodsRule(timePeriods)];
        }
    }
}

