// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Application.Dtos
{
    public class RankingDto
    {
        public List<RankingRowDto>? Rows { get; set; }

        public RankingRules? Rules { get; set; }

        public Dictionary<Guid, int>? PenaltyPoints { get; set; }

        public Dictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; set; }
    }
}
