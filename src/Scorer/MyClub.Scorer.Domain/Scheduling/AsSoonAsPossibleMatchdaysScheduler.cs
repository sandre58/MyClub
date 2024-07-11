// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class AsSoonAsPossibleMatchdaysScheduler : IMatchdaysScheduler
    {
        public static readonly AsSoonAsPossibleMatchdaysScheduler Default = new();

        public DateTime StartDate { get; set; } = DateTime.Now;

        public List<IAvailableDateRule> Rules { get; } = [];

        public void Schedule(IEnumerable<SchedulingMatchdayInformation> matchdays)
        {
            var scheduledMatches = new List<Match>();
            matchdays.ForEach(x =>
            {
                x.Matchday.Matches.ForEach((match, matchIndex) =>
                {
                    var lastMatchOfHomeTeam = scheduledMatches.Where(y => y.Participate(match.HomeTeam)).OrderBy(x => x.Date).LastOrDefault()?.GetPeriod().End.AddFluentTimeSpan(x.SchedulingParameters.RestTime);
                    var lastMatchOfAwayTeam = scheduledMatches.Where(y => y.Participate(match.AwayTeam)).OrderBy(x => x.Date).LastOrDefault()?.GetPeriod().End.AddFluentTimeSpan(x.SchedulingParameters.RestTime);

                    var dateOfMatch = lastMatchOfHomeTeam.HasValue || lastMatchOfAwayTeam.HasValue
                        ? DateTimeHelper.Max(lastMatchOfAwayTeam.GetValueOrDefault(), lastMatchOfHomeTeam.GetValueOrDefault())
                        : StartDate;
                    dateOfMatch = ApplyRules(dateOfMatch, match.Format.GetFullTime());

                    match.Schedule(dateOfMatch);
                });

                var dateOfMatchday = x.Matchday.Matches.MinOrDefault(x => x.Date, x.Matchday.Date);
                x.Matchday.Schedule(dateOfMatchday);
            });
        }

        private DateTime ApplyRules(DateTime dateOfMatch, TimeSpan matchDuration)
        {
            const int maxComputing = 10;
            var mustBeComputed = true;
            var computedDate = dateOfMatch;
            var countComputing = 1;
            while (mustBeComputed)
            {
                if (countComputing >= maxComputing)
                    throw new TranslatableException("MaxComputingAttemptExceededError");

                var previousComputedDate = computedDate;

                foreach (var rule in Rules)
                    computedDate = rule.GetNextAvailableDate(new Period(computedDate, computedDate.AddFluentTimeSpan(matchDuration)));
                mustBeComputed = previousComputedDate != computedDate;
                countComputing++;
            }

            return computedDate;
        }
    }

    public interface IAvailableDateRule
    {
        DateTime GetNextAvailableDate(Period matchPeriod);
    }

    public class IncludeTimePeriodsRule : IAvailableDateRule
    {
        private readonly IEnumerable<TimePeriod> _periods;

        public IncludeTimePeriodsRule(IEnumerable<TimePeriod> periods) => _periods = periods;

        public DateTime GetNextAvailableDate(Period matchPeriod)
            => _periods.MinOrDefault(x => GetNextAvailableDate(x, matchPeriod), matchPeriod.Start);

        private static DateTime GetNextAvailableDate(TimePeriod utcPeriod, Period matchPeriod)
        {
            var matchDate = matchPeriod.Start.Date;
            var matchStartTime = matchPeriod.Start.TimeOfDay;
            var matchEndTime = matchPeriod.End.TimeOfDay;
            var isInPeriod = matchEndTime > matchStartTime && new Period(matchDate.ToUtcDateTime(utcPeriod.Start), matchDate.ToUtcDateTime(utcPeriod.End)).Contains(matchPeriod);

            if (isInPeriod)
                return matchPeriod.Start;

            if (utcPeriod.End - utcPeriod.Start < matchPeriod.Duration)
                throw new TranslatableException("DefinedPeriodIsLowerThanMatchPeriodError");

            if (matchStartTime > utcPeriod.Start)
                matchDate = matchDate.AddDays(1);

            return matchDate.ToUtcDateTime(utcPeriod.Start);
        }
    }

    public class IncludeDaysOfWeekRule : IAvailableDateRule
    {
        private readonly IEnumerable<DayOfWeek> _dayOfWeeks;

        public IncludeDaysOfWeekRule(IEnumerable<DayOfWeek> dayOfWeeks) => _dayOfWeeks = dayOfWeeks;

        public DateTime GetNextAvailableDate(Period matchPeriod)
            => !_dayOfWeeks.Any() || _dayOfWeeks.Contains(matchPeriod.Start.DayOfWeek) ? matchPeriod.Start : NextDay(matchPeriod.Start.Date);

        public DateTime NextDay(DateTime start)
        {
            do
            {
                start = start.NextDay();
            } while (!_dayOfWeeks.Contains(start.DayOfWeek));

            return start;
        }
    }

    public class ExcludePeriodRule : IAvailableDateRule
    {
        private readonly Period _period;

        public ExcludePeriodRule(Period period) => _period = period;

        public DateTime GetNextAvailableDate(Period matchPeriod) => _period.Intersect(matchPeriod) ? _period.End.NextDay().BeginningOfDay() : matchPeriod.Start;
    }
}

