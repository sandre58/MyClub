// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Application.Dtos
{
    public class BuildParametersDto
    {
        public ChampionshipAlgorithm Algorithm { get; set; }

        public int NumberOfMatchesByTeam { get; set; }

        public bool[]? MatchesBetweenTeams { get; set; }

        public MatchFormat? MatchFormat { get; set; }

        public SchedulingParameters? SchedulingParameters { get; set; }

        public BuildDatesParametersDto? BuildDatesParameters { get; set; }

        public string? NamePattern { get; set; }

        public string? ShortNamePattern { get; set; }
    }

    public abstract class BuildDatesParametersDto { }

    public class BuildAsSoonAsPossibleParametersDto : BuildDatesParametersDto
    {
        public DateTime? StartDate { get; set; }

        public List<IAvailableDateRule>? Rules { get; set; }
    }

    public class BuildAutomaticParametersDto : BuildDatesParametersDto
    {
        public List<(DateTime date, IEnumerable<DateTime> datesOfMatches)>? Dates { get; set; }
    }

    public class BuildManualParametersDto : BuildDatesParametersDto
    {
        public List<(DateTime date, IEnumerable<DateTime> datesOfMatches)>? Dates { get; set; }
    }
}
