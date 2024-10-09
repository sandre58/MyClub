// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Scorer.Application.Dtos
{
    public class MatchdaysDto : EntityDto
    {
        public Guid? StageId { get; set; }

        public DateTime? StartDate { get; set; }

        public List<MatchdayDto>? Matchdays { get; set; }

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumsAutomatic { get; set; }
    }
}
