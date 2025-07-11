// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;

namespace MyClub.Scorer.Application.Dtos
{
    public class RoundStageDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public DateTime Date { get; set; }

        public bool IsPostponed { get; set; }

        public DateTime? PostponedDate { get; set; }

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumsAutomatic { get; set; }
    }
}
