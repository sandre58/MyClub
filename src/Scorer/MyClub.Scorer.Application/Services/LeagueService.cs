// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;

namespace MyClub.Scorer.Application.Services
{
    public class LeagueService(IProjectRepository projectRepository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;

        public void UpdateRankingRules(RankingRulesDto dto)
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            if (dto.Labels is not null)
            {
                league.Labels.Clear();
                league.Labels.AddRange(dto.Labels);
            }

            if (dto.PenaltyPoints is not null)
            {
                league.ClearPenaltyPoints();

                dto.PenaltyPoints.ForEach(x => league.AddPenalty(x.Key, x.Value));
            }

            if (dto.Rules is not null)
                league.RankingRules = dto.Rules;
        }

        public MatchFormat GetMatchFormat()
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            return league.MatchFormat;
        }

        public RankingRulesDto GetRankingRules()
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            return new()
            {
                Labels = league.Labels.ToDictionary(x => x.Key, x => x.Value),
                PenaltyPoints = league.GetPenaltyPoints().ToDictionary(x => x.Key.Id, x => x.Value),
                Rules = league.RankingRules,
            };
        }

        public RankingDto GetRanking(Guid matchdayId)
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");
            var matchday = league.Matchdays.GetById(matchdayId);

            return ToDto(league.GetRanking(matchday));
        }

        public RankingDto GetRanking(bool live = false)
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            var previousRanking = live ? league.GetRanking() : null;
            var ranking = live ? league.GetLiveRanking() : league.GetRanking();

            return ToDto(ranking, previousRanking);
        }

        public RankingDto GetHomeRanking()
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            return ToDto(league.GetHomeRanking());
        }

        public RankingDto GetAwayRanking()
        {
            var league = _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

            return ToDto(league.GetAwayRanking());
        }

        private static RankingDto ToDto(Ranking ranking, Ranking? previousRanking = null) => new()
        {
            Labels = ranking.Labels?.ToDictionary(x => x.Key, x => x.Value),
            PenaltyPoints = ranking.PenaltyPoints?.ToDictionary(x => x.Key.Id, x => x.Value),
            Rules = ranking.Rules,
            Rows = ranking.Select(x => new RankingRowDto
            {
                Points = x.GetPoints(),
                Rank = ranking.GetRank(x.Team),
                Label = ranking.GetLabel(x.Team),
                TeamId = x.Team.Id,
                MatchIds = x.GetMatches().Select(y => y.Id).ToList(),
                Columns = ranking.Rules.Computers.ToDictionary(y => y.Key, y => x.Get(y.Key)),
                Progression = previousRanking is not null ? previousRanking.GetRank(x.Team) - ranking.GetRank(x.Team) : null,
            }).ToList()
        };
    }
}
