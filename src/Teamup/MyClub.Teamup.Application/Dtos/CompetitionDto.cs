// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class CompetitionDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public Category? Category { get; set; }

        public CompetitionRules? Rules { get; set; }

        public Guid SeasonId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<TeamDto>? Teams { get; set; }
    }
}
