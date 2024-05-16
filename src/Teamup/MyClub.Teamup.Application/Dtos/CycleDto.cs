// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Teamup.Application.Dtos
{
    public class CycleDto : EntityDto
    {
        public string? Label { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? Color { get; set; }

        public List<string> TechnicalGoals { get; set; } = [];

        public List<string> TacticalGoals { get; set; } = [];

        public List<string> PhysicalGoals { get; set; } = [];

        public List<string> MentalGoals { get; set; } = [];
    }
}
