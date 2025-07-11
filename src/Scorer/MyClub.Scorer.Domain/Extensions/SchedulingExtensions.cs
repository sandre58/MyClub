// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class SchedulingExtensions
    {
        public static DateOnly GetCurrentStartDate(this SchedulingParameters parameters) => parameters.Start().ToCurrentTime().ToDate();

        public static DateOnly GetCurrentEndDate(this SchedulingParameters parameters) => parameters.EndDate.At(parameters.StartTime).ToCurrentTime().ToDate();

        public static TimeOnly GetCurrentStartTime(this SchedulingParameters parameters) => parameters.Start().ToCurrentTime().ToTime();

        public static bool CanAutomaticReschedule(this SchedulingParameters parameters) => parameters.AsSoonAsPossible || parameters.DateRules.Count > 0;

        public static bool CanAutomaticRescheduleVenue(this SchedulingParameters parameters) => parameters.UseHomeVenue || parameters.VenueRules.Count > 0 || parameters.AsSoonAsPossible;

        public static void ScheduleVenues(this SchedulingParameters parameters, IEnumerable<Match> matches, IEnumerable<Stadium> stadiums, IEnumerable<Match>? scheduledMatches = null)
            => parameters.GetVenuesScheduler(stadiums, scheduledMatches)?.Schedule(matches.ToList());

        public static IVenueScheduler? GetVenuesScheduler(this SchedulingParameters parameters, IEnumerable<Stadium> stadiums, IEnumerable<Match>? scheduledMatches = null)
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

        public static void Schedule(this SchedulingParameters parameters, IEnumerable<IMatchesStage> stages, DateTime? startDate = null, IEnumerable<IMatchesStage>? scheduledStages = null)
            => parameters.GetScheduler(startDate, scheduledStages)?.Schedule(stages.ToList());

        public static IScheduler<Match>? GetMatchesScheduler(this SchedulingParameters parameters, DateTime? startDate = null, IEnumerable<Match>? scheduledMatches = null)
        {
            if (parameters.AsSoonAsPossible)
                return new AsSoonAsPossibleMatchesScheduler(startDate ?? parameters.Start(), scheduledMatches)
                {
                    Rules = [.. parameters.AsSoonAsPossibleRules],
                    ScheduleVenues = false
                };
            else if (parameters.DateRules.Count > 0)
            {
                return new DateRulesMatchesScheduler((startDate?.ToDate() ?? parameters.StartDate).BeginningOfDay().Add(parameters.Interval).ToDate())
                {
                    ScheduleByStage = parameters.ScheduleByStage,
                    Interval = parameters.Interval,
                    DateRules = [.. parameters.DateRules],
                    TimeRules = [.. parameters.TimeRules],
                    DefaultTime = parameters.StartTime,

                };
            }

            return null;
        }

        public static IDateScheduler<IMatchesStage>? GetScheduler(this SchedulingParameters parameters, DateTime? startDate = null, IEnumerable<IMatchesStage>? scheduledItems = null)
        {
            if (parameters.AsSoonAsPossible)
                return new AsSoonAsPossibleStageScheduler(startDate ?? parameters.Start(), scheduledItems)
                {
                    Rules = [.. parameters.AsSoonAsPossibleRules],
                    ScheduleVenues = false
                };
            else if (parameters.DateRules.Count > 0)
                return new DateRulesStageScheduler(startDate?.ToDate() ?? parameters.StartDate)
                {
                    Interval = parameters.Interval,
                    DateRules = [.. parameters.DateRules],
                    TimeRules = [.. parameters.TimeRules],
                    DefaultTime = parameters.StartTime,
                };

            return null;
        }
    }
}
