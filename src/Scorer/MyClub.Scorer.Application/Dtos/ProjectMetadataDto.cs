// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Application.Dtos
{
    public class ProjectMetadataDto
    {
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public PreferencesDto? Preferences { get; set; }

        public List<StadiumDto>? Stadiums { get; set; }

        public List<TeamDto>? Teams { get; set; }

        public MatchRules? MatchRules { get; set; }

        public BuildParametersDto? BuildParameters { get; set; }
    }

    public class LeagueMetadataDto : ProjectMetadataDto
    {
        public RankingRulesDto? RankingRules { get; set; }
    }

    public class CupMetadataDto : ProjectMetadataDto { }

    public class TournamentMetadataDto : ProjectMetadataDto { }
}
