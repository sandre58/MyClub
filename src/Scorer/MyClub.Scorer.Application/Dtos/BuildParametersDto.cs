// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Application.Dtos
{
    public class BuildParametersDto
    {
        public ChampionshipAlgorithm Algorithm { get; set; }

        public int NumberOfMatchesByTeam { get; set; }

        public bool[]? MatchesBetweenTeams { get; set; }

        public MatchFormat? MatchFormat { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan RotationTime { get; set; }

        public TimeSpan RestTime { get; set; }

        public bool UseHomeVenue { get; set; }

        public bool AsSoonAsPossible { get; set; }

        public TimeSpan Interval { get; set; }

        public List<IAvailableVenueSchedulingRule>? VenueRules { get; set; }

        public List<IAvailableDateSchedulingRule>? AsSoonAsPossibleRules { get; set; }

        public List<IDateSchedulingRule>? DateRules { get; set; }

        public List<ITimeSchedulingRule>? TimeRules { get; set; }

        public BuildDatesParametersDto? BuildDatesParameters { get; set; }

        public string? NamePattern { get; set; }

        public string? ShortNamePattern { get; set; }

        public bool ScheduleVenues { get; set; }

        public bool AsSoonAsPossibleVenues { get; set; }

        public bool ScheduleVenuesBeforeDates { get; set; }
    }

    public abstract class BuildDatesParametersDto { }

    public class BuildAsSoonAsPossibleDatesParametersDto : BuildDatesParametersDto
    {
        public DateTime? StartDate { get; set; }

        public List<IAvailableDateSchedulingRule>? Rules { get; set; }
    }

    public class BuildAutomaticDatesParametersDto : BuildDatesParametersDto
    {
        public DateTime? StartDate { get; set; }

        public TimeSpan? DefaultTime { get; set; }

        public int IntervalValue { get; set; }

        public TimeUnit IntervalUnit { get; set; }

        public List<IDateSchedulingRule>? DateRules { get; set; }

        public List<ITimeSchedulingRule>? TimeRules { get; set; }
    }

    public class BuildManualDatesParametersDto : BuildDatesParametersDto
    {
        public List<(DateTime date, IEnumerable<DateTime> datesOfMatches)>? Dates { get; set; }
    }
}
