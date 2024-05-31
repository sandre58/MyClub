// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class CompetitionImportDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public string? Category { get; set; }

        public string? Type { get; set; }

        public TimeSpan? MatchTime { get; set; }

        public bool HasExtraTime { get; set; }

        public bool HasShootouts { get; set; }

        public HalfFormatImportDto? RegulationTime { get; set; }

        public HalfFormatImportDto? ExtraTime { get; set; }

        public int? NumberOfShootouts { get; set; }

        public int? ByGamesWon { get; set; }

        public int? ByGamesDrawn { get; set; }

        public int? ByGamesLost { get; set; }

        public IEnumerable<string>? RankingSortingColumns { get; set; }

        public IDictionary<(int? min, int? max), RankLabelImportDto>? Labels { get; set; }
    }
}
