// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class SchedulingExtensions
    {
        public static void ScheduleVenues(this SchedulingParameters parameters, IEnumerable<Match> matches, IEnumerable<Stadium> stadiums, IEnumerable<Match>? scheduledMatches = null)
            => parameters.GetVenuesScheduler(stadiums, scheduledMatches)?.Schedule(matches.ToList());

        public static IMatchesScheduler? GetVenuesScheduler(this SchedulingParameters parameters, IEnumerable<Stadium> stadiums, IEnumerable<Match>? scheduledMatches = null)
        {
            if (parameters.UseHomeVenue)
                return new HomeTeamVenueMatchesScheduler();
            else if (parameters.VenueRules.Count != 0)
                return new VenueRulesMatchesScheduler(stadiums, scheduledMatches)
                {
                    Rules = [.. parameters.VenueRules],
                };
            else if (parameters.AsSoonAsPossible)
                return new VenueRulesMatchesScheduler(stadiums, scheduledMatches)
                {
                    Rules = [new FirstAvailableStadiumRule(UseRotationTime.Yes)],
                };

            return null;
        }

        public static void Schedule(this SchedulingParameters parameters, IEnumerable<Match> matches, DateTime? startDate = null, IEnumerable<Match>? scheduledMatches = null)
            => parameters.GetMatchesScheduler(startDate, scheduledMatches)?.Schedule(matches.ToList());

        public static IMatchesScheduler? GetMatchesScheduler(this SchedulingParameters parameters, DateTime? startDate = null, IEnumerable<Match>? scheduledMatches = null)
        {
            if (parameters.AsSoonAsPossible)
                return new AsSoonAsPossibleMatchesScheduler(scheduledMatches)
                {
                    StartDate = startDate ?? parameters.StartDate,
                    Rules = [.. parameters.AsSoonAsPossibleRules],
                    ScheduleVenues = false
                };
            else if (parameters.DateRules.Count > 0)
            {
                return new DateRulesMatchesScheduler()
                {
                    ScheduleByParent = parameters.ScheduleByParent,
                    Interval = parameters.Interval,
                    DateRules = [.. parameters.DateRules],
                    TimeRules = [.. parameters.TimeRules],
                    DefaultTime = parameters.StartTime,
                    StartDate = (startDate ?? parameters.StartDate).Add(parameters.Interval),
                };
            }

            return null;
        }

        public static IScheduler<Matchday>? GetMatchdaysScheduler(this SchedulingParameters parameters, DateTime? startDate = null, IEnumerable<Matchday>? scheduledMatchdays = null)
        {
            if (parameters.AsSoonAsPossible)
                return new AsSoonAsPossibleScheduler<Matchday>(scheduledMatchdays)
                {
                    StartDate = startDate ?? parameters.StartDate,
                    Rules = [.. parameters.AsSoonAsPossibleRules],
                    ScheduleVenues = false
                };
            else if (parameters.DateRules.Count > 0)
                return new DateRulesScheduler<Matchday>()
                {
                    Interval = parameters.Interval,
                    DateRules = [.. parameters.DateRules],
                    TimeRules = [.. parameters.TimeRules],
                    DefaultTime = parameters.StartTime,
                    StartDate = startDate ?? parameters.StartDate,
                };

            return null;
        }
    }
}
