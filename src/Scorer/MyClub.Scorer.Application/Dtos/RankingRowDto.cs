// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.RankingAggregate;

namespace MyClub.Scorer.Application.Dtos
{
    public class RankingRowDto
    {
        public Guid? TeamId { get; set; }

        public int Rank { get; set; }

        public int? Progression { get; set; }

        public int Points { get; set; }

        public RankLabel? Label { get; set; }

        public Dictionary<string, object?>? Columns { get; set; }

        public List<Guid>? MatchIds { get; set; }
    }
}
