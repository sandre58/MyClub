// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class TimeOfDayRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfDayRule(DayOfWeek day, TimeOnly time, IEnumerable<TimeOfIndexRule>? exceptions = null)
        {
            Day = day;
            Time = time;
            Exceptions = new List<TimeOfIndexRule>(exceptions ?? []).AsReadOnly();
        }

        public DayOfWeek Day { get; }

        public TimeOnly Time { get; }

        public IReadOnlyCollection<TimeOfIndexRule> Exceptions { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => date.DayOfWeek == Day
                ? Exceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDayRule(Day, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), Exceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfIndexRule>())];
    }

    public class TimeOfDateRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfDateRule(DateOnly date, TimeOnly time, IEnumerable<TimeOfIndexRule>? exceptions = null)
        {
            Date = date;
            Time = time;
            Exceptions = new List<TimeOfIndexRule>(exceptions ?? []).AsReadOnly();
        }

        public DateOnly Date { get; }

        public TimeOnly Time { get; }

        public IReadOnlyCollection<TimeOfIndexRule> Exceptions { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => date == Date
                ? Exceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDateRule(Date, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), Exceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfIndexRule>())];
    }

    public class TimeOfIndexRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfIndexRule(int index, TimeOnly time)
        {
            Index = index;
            Time = time;
        }

        public int Index { get; }

        public TimeOnly Time { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index) => Index == index ? Time : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => [new TimeOfIndexRule(Index, Time.ToTimeZone(sourceTimeZone, destinationTimeZone))];
    }

    public class TimeOfDatesRangeRule : ITimeSchedulingRule
    {
        public TimeOfDatesRangeRule(DateOnly startDate, DateOnly endDate, TimeOnly time, IEnumerable<TimeOfIndexRule>? exceptions = null)
        {
            StartDate = startDate;
            EndDate = endDate;
            Time = time;
            Exceptions = new List<TimeOfIndexRule>(exceptions ?? []).AsReadOnly();
        }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly Time { get; set; }

        public IReadOnlyCollection<TimeOfIndexRule> Exceptions { get; } = [];

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => new DatePeriod(StartDate, EndDate).Contains(date)
                ? Exceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;
        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDatesRangeRule(StartDate, EndDate, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), Exceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfIndexRule>())];
    }
}

