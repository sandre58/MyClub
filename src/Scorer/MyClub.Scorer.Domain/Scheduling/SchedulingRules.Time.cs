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
        public TimeOfDayRule(DayOfWeek day, TimeSpan time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            Day = day;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DayOfWeek Day { get; }

        public TimeSpan Time { get; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; }

        public TimeSpan? ProvideTime(DateTime date, int index)
            => date.DayOfWeek == Day
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time
                : null;
    }

    public class TimeOfDateRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfDateRule(DateTime date, TimeSpan time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            Date = date;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateTime Date { get; }

        public TimeSpan Time { get; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; }

        public TimeSpan? ProvideTime(DateTime date, int index)
            => date == Date
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time
                : null;
    }

    public class TimeOfMatchNumberRule : ValueObject, ITimeSchedulingRule
    {
        public TimeOfMatchNumberRule(int matchNumber, TimeSpan time)
        {
            MatchNumber = matchNumber;
            Time = time;
        }

        public int MatchNumber { get; }

        public TimeSpan Time { get; }

        public TimeSpan? ProvideTime(DateTime date, int index) => MatchNumber == index + 1 ? Time : null;
    }

    public class TimeOfDatesRangeRule : ITimeSchedulingRule
    {
        public TimeOfDatesRangeRule(DateTime startDate, DateTime endDate, TimeSpan time, IEnumerable<TimeOfMatchNumberRule>? matchExceptions = null)
        {
            StartDate = startDate;
            EndDate = endDate;
            Time = time;
            MatchExceptions = new List<TimeOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public TimeSpan Time { get; set; }

        public IReadOnlyCollection<TimeOfMatchNumberRule> MatchExceptions { get; } = [];

        public TimeSpan? ProvideTime(DateTime date, int index)
            => new Period(StartDate, EndDate).Contains(date)
                ? MatchExceptions.Select(x => x.ProvideTime(date, index)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time
                : null;
    }
}

