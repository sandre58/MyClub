// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

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
        public MatchdaysAlgorithmDto? AlgorithmParameters { get; set; }
    }

    public class BuildRoundsParametersDto : BuildBracketParametersDto
    {
        public RoundsAlgorithmDto? AlgorithmParameters { get; set; }
    }
}
