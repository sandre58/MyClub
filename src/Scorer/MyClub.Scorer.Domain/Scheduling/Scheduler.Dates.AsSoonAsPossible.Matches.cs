// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class AsSoonAsPossibleMatchesScheduler : IMatchesScheduler
    {
        private readonly IEnumerable<Match> _scheduledMatches;

        public AsSoonAsPossibleMatchesScheduler(IEnumerable<Match>? scheduledMatches = null) => _scheduledMatches = scheduledMatches ?? [];

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public List<IAvailableDateSchedulingRule> Rules { get; set; } = [];

        public bool ScheduleVenues { get; set; } = true;

        public ICollection<Stadium> AvailableStadiums { get; set; } = [];

        public void Schedule(IEnumerable<Match> matches)
        {
            var newScheduledMatches = new List<Match>(_scheduledMatches.Except(matches));
            matches.ForEach(x =>
            {
                // 1 - Create available date ranges
                var homeTeamAvailabilities = GetTeamAvailabilities(x.HomeTeam, newScheduledMatches);
                var awayTeamAvailabilities = GetTeamAvailabilities(x.AwayTeam, newScheduledMatches);
                var stadiumsAvailabilities = ScheduleVenues switch
                {
                    true => AvailableStadiums.ToDictionary(y => y, y => GetStadiumAvailabilities(y, newScheduledMatches)),
                    false => x.Stadium is not null ? new Dictionary<Stadium, Availabilities>
                    {
                        { x.Stadium,  GetStadiumAvailabilities(x.Stadium, newScheduledMatches)}
                    } : [],
                };

                // 2 - Find commmon availabilities
                var commonTeamsAvailabilities = GetCommonAvailabilities(homeTeamAvailabilities, awayTeamAvailabilities);
                var commmonAvailabilitiesByStadium = stadiumsAvailabilities.Select(y => new { Stadium = y.Key, Availabilities = GetCommonAvailabilities(y.Value, commonTeamsAvailabilities) }).ToList();

                // 3.1 - Find first availability between teams and stadium
                var result = commmonAvailabilitiesByStadium.Select(y => new { y.Stadium, Date = GetFirstAvailableDate(y.Availabilities, x) })
                                                           .OrderBy(x => x.Date)
                                                           .FirstOrDefault();
                if (result is not null && result.Date != default)
                    ApplyAvailability(x, result.Date, result.Stadium);

                // 3.2 - Find first availability between teams
                else
                {
                    var firstDate = GetFirstAvailableDate(commonTeamsAvailabilities, x);

                    if (firstDate != default)
                        ApplyAvailability(x, firstDate);

                    // 3.3 - Find last common availability date
                    else
                    {
                        ApplyAvailability(x, commonTeamsAvailabilities.LastDate);
                    }
                }
                newScheduledMatches.Add(x);
            });
        }

        private void ApplyAvailability(Match match, DateTime date, Stadium? stadium = null)
        {
            if (ScheduleVenues && stadium is not null)
            {
                match.Stadium = stadium;
                match.IsNeutralStadium = true;
            }
            match.Schedule(date);
        }

        private DateTime GetFirstAvailableDate(Availabilities availabilities, Match match, bool greaterThanMatchDuration = true)
        {
            var periods = availabilities.Periods.Where(x => !greaterThanMatchDuration || x.Duration >= match.Format.GetFullTime()).ToList();

            return periods.Count > 0 ? periods.Min(x => x.Start) : ApplyRules(availabilities.LastDate, match.Format.GetFullTime());
        }

        private static Availabilities GetCommonAvailabilities(Availabilities availabilities1, Availabilities availabilities2)
        {
            var periods = new List<Period>();
            foreach (var period1 in availabilities1.Periods)
            {
                foreach (var period2 in availabilities2.Periods)
                {
                    var startNewAvailability = period2.Start > period1.Start ? period2.Start : period1.Start;
                    var endNewAvailability = period2.End < period1.End ? period2.End : period1.End;

                    if (period1.Intersect(period2) is Period commonPeriod)
                        periods.Add(commonPeriod);
                    else if (period1.Start > availabilities2.LastDate)
                        periods.Add(period1);
                    else if (period2.Start > availabilities1.LastDate)
                        periods.Add(period2);
                    else if (period1.Contains(availabilities2.LastDate))
                        periods.Add(availabilities2.LastDate.ToPeriod(period1.End));
                    else if (period2.Contains(availabilities1.LastDate))
                        periods.Add(availabilities1.LastDate.ToPeriod(period2.End));
                }
            }

            return (periods.Merge<DateTime, Period>().Select(x => new Period(x.Start, x.End)).ToList(), DateTimeHelper.Max(availabilities1.LastDate, availabilities2.LastDate));
        }

        private Availabilities GetStadiumAvailabilities(Stadium stadium, IEnumerable<Match> scheduledMatches)
            => GetAvailabilities(scheduledMatches.Where(x => x.State != MatchState.Cancelled && x.Stadium is not null && x.Stadium == stadium), x => x.GetRotationTime());

        private Availabilities GetTeamAvailabilities(ITeam team, IEnumerable<Match> scheduledMatches)
            => GetAvailabilities(scheduledMatches.Where(x => x.State != MatchState.Cancelled && x.Participate(team)), x => x.GetRestTime());

        private Availabilities GetAvailabilities(IEnumerable<Match> matches, Func<Match, TimeSpan> provideOffsetTime)
        {
            var result = new List<Period>();

            var endPreviousUnavailableDate = StartDate;

            foreach (var match in matches.OrderBy(x => x.Date).ToList())
            {
                var startNewUnavailableDate = match.Date.Subtract(provideOffsetTime(match));

                if (startNewUnavailableDate > endPreviousUnavailableDate)
                    result.Add(new Period(endPreviousUnavailableDate, startNewUnavailableDate));

                endPreviousUnavailableDate = match.Date.Add(match.Format.GetFullTime()).Add(provideOffsetTime(match));
            }

            return (result.SelectMany(ApplyRules).ToList(), endPreviousUnavailableDate);
        }

        private IEnumerable<Period> ApplyRules(Period period)
        {
            var result = new List<Period>();
            var periods = new List<Period>() { period };
            foreach (var rule in Rules)
                periods = periods.SelectMany(rule.GetAvailablePeriods).ToList();

            return periods.Merge<DateTime, Period>();
        }

        private DateTime ApplyRules(DateTime date, TimeSpan matchDuration)
        {
            const int maxComputing = 10;
            var mustBeComputed = true;
            var computedDate = date;
            var countComputing = 1;
            while (mustBeComputed)
            {
                if (countComputing >= maxComputing)
                    throw new TranslatableException("MaxComputingAttemptExceededError");

                var previousComputedDate = computedDate;

                foreach (var rule in Rules)
                    computedDate = rule.GetNextAvailableDate(computedDate.ToPeriod(matchDuration));
                mustBeComputed = previousComputedDate != computedDate;
                countComputing++;
            }

            return computedDate;
        }

        private record struct Availabilities(List<Period> Periods, DateTime LastDate)
        {
            public static implicit operator (List<Period> availabilities, DateTime lastDate)(Availabilities value) => (value.Periods, value.LastDate);
            public static implicit operator Availabilities((List<Period> availabilities, DateTime lastDate) value) => new(value.availabilities, value.lastDate);
        }
    }
}

