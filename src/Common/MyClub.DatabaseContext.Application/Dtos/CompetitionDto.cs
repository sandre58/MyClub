// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.DatabaseContext.Application.Dtos
{
    public class CompetitionDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public Category? Category { get; set; }

        public int RegulationTimeNumber { get; set; }

        public TimeSpan RegulationTimeDuration { get; set; }

        public int? ExtraTimeNumber { get; set; }

        public TimeSpan? ExtraTimeDuration { get; set; }

        public int? NumberOfPenaltyShootouts { get; set; }

        public TimeSpan MatchTime { get; set; }
    }
}
