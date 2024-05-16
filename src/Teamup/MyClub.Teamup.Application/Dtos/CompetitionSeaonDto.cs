// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Application.Dtos
{
    public class CompetitionSeasonDto : EntityDto
    {
        public CompetitionDto? Competition { get; set; }

        public Guid SeasonId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<TeamDto>? Teams { get; set; }

        public MatchFormat? MatchFormat { get; set; }

        public TimeSpan MatchTime { get; set; }
    }
}
