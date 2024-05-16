// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Application.Dtos
{
    public class RankingRulesDto
    {
        public RankingRules? Rules { get; set; }

        public Dictionary<Guid, int>? PenaltyPoints { get; set; }

        public Dictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; set; }
    }
}
