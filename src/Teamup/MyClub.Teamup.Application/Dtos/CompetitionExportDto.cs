// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Application.Dtos
{
    public class CompetitionExportDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public Category? Category { get; set; }

        public string? Type { get; set; }

        public TimeSpan? MatchTime { get; set; }

        public bool HasExtraTime { get; set; }

        public bool HasShootouts { get; set; }

        public HalfFormat? RegulationTime { get; set; }

        public HalfFormat? ExtraTime { get; set; }

        public int? NumberOfShootouts { get; set; }

        public int? ByGamesWon { get; set; }

        public int? ByGamesDrawn { get; set; }

        public int? ByGamesLost { get; set; }

        public IEnumerable<RankingSortingColumn>? RankingSortingColumns { get; set; }

        public IDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; set; }
    }
}
