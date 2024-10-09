// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Application.Dtos
{
    public class PlayerStatisticsDto
    {
        public Guid PlayerId { get; set; }

        public Guid TeamId { get; set; }

        public int Goals { get; set; }

        public int Assists { get; set; }

        public Dictionary<CardColor, int>? Cards { get; set; }
    }
}
