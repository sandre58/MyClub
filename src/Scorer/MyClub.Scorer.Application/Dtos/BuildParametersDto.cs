// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Application.Dtos
{
    public class BuildParametersDto
    {
        public BuildBracketParametersDto? BracketParameters { get; set; }

        public MatchFormat? MatchFormat { get; set; }

        public SchedulingParameters? SchedulingParameters { get; set; }

        public bool AutomaticStartDate { get; set; }

        public bool AutomaticEndDate { get; set; }
    }

    public class BuildBracketParametersDto
    {
        public BuildAlgorithmParametersDto? AlgorithmParameters { get; set; }

        public BuildDatesParametersDto? BuildDatesParameters { get; set; }

        public string? NamePattern { get; set; }

        public string? ShortNamePattern { get; set; }

        public bool ScheduleVenues { get; set; }

        public bool AsSoonAsPossibleVenues { get; set; }

        public bool ScheduleVenuesBeforeDates { get; set; }

        public bool UseHomeVenue { get; set; }

        public List<IAvailableVenueSchedulingRule>? VenueRules { get; set; }
    }

    public class BuildMatchdaysParametersDto : BuildBracketParametersDto
    {
    }

    public abstract class BuildAlgorithmParametersDto
    {
        public int NumberOfTeams { get; set; }
    }

    public class RoundRobinParametersDto : BuildAlgorithmParametersDto
    {
        public bool[]? MatchesBetweenTeams { get; set; }
    }

    public class SwissSystemParametersDto : BuildAlgorithmParametersDto
    {
        public int NumberOfMatchesByTeam { get; set; }
    }

    public abstract class BuildDatesParametersDto { }

    public class BuildAsSoonAsPossibleDatesParametersDto : BuildDatesParametersDto
    {
        public DateTime? StartDate { get; set; }

        public List<IAvailableDateSchedulingRule>? Rules { get; set; }
    }

    public class BuildAutomaticDatesParametersDto : BuildDatesParametersDto
    {
        public DateOnly? StartDate { get; set; }

        public TimeOnly? DefaultTime { get; set; }

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
