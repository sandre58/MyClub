// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Teamup.Application.Dtos
{
    public class TrainingSessionDto : EntityDto
    {
        public string? Theme { get; set; }

        public string? Place { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public virtual bool IsCancelled { get; set; }

        public List<Guid>? TeamIds { get; set; }

        public List<TrainingAttendanceDto>? Attendances { get; set; }

        public List<string>? Stages { get; set; }

        public List<string>? TechnicalGoals { get; set; }

        public List<string>? TacticalGoals { get; set; }

        public List<string>? PhysicalGoals { get; set; }

        public List<string>? MentalGoals { get; set; }
    }
}
