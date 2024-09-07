// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class TimeOfDayRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfDayRule(DayOfWeek day, TimeOnly time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            Day = day;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DayOfWeek Day { get; }

        public TimeOnly Time { get; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => date.DayOfWeek == Day
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDayRule(Day, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), MatchExceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfMatchNumberRule>())];
    }

    public class TimeOfDateRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfDateRule(DateOnly date, TimeOnly time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            Date = date;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateOnly Date { get; }

        public TimeOnly Time { get; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => date == Date
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDateRule(Date, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), MatchExceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfMatchNumberRule>())];
    }

    public class TimeOfMatchNumberRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfMatchNumberRule(int matchNumber, TimeOnly time)
        {
            MatchNumber = matchNumber;
            Time = time;
        }

        public int MatchNumber { get; }

        public TimeOnly Time { get; }

        public TimeOnly? ProvideTime(DateOnly date, int index) => MatchNumber == index + 1 ? Time : null;

        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) => [new TimeOfMatchNumberRule(MatchNumber, Time.ToTimeZone(sourceTimeZone, destinationTimeZone))];
    }

    public class TimeOfDatesRangeRule : ITimeSchedulingRule
    {
        public TimeOfDatesRangeRule(DateOnly startDate, DateOnly endDate, TimeOnly time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            StartDate = startDate;
            EndDate = endDate;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly Time { get; set; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; } = [];

        public TimeOnly? ProvideTime(DateOnly date, int index)
            => new DatePeriod(StartDate, EndDate).Contains(date)
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeOnly time ? time : Time
                : null;
        public IEnumerable<ITimeSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
            => [new TimeOfDatesRangeRule(StartDate, EndDate, Time.ToTimeZone(sourceTimeZone, destinationTimeZone), MatchExceptions.SelectMany(x => x.ConvertToTimeZone(sourceTimeZone, destinationTimeZone)).OfType<TimeOfMatchNumberRule>())];
    }
}

