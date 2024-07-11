// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class LeagueService(ILeagueRepository leagueRepository, ISchedulingParametersRepository schedulingParametersRepository, IMatchdayRepository matchdayRepository)
    {
        private readonly ISchedulingParametersRepository _schedulingParametersRepository = schedulingParametersRepository;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;
        private readonly IMatchdayRepository _matchdayRepository = matchdayRepository;

        public void UpdateRankingRules(RankingRulesDto dto)
        {
            if (dto.Rules is not null)
                _leagueRepository.UpdateRankingRules(dto.Rules);

            if (dto.Labels is not null)
                _leagueRepository.UpdateLabels(dto.Labels);

            if (dto.PenaltyPoints is not null)
                _leagueRepository.UpdatePenaltyPoints(dto.PenaltyPoints);
        }

        public MatchFormat GetMatchFormat() => _leagueRepository.GetCurrentOrThrow().MatchFormat;

        public int GetRankingRowsCount() => _leagueRepository.GetCurrentOrThrow().Teams.Count;

        public RankingRulesDto GetRankingRules()
        {
            var league = _leagueRepository.GetCurrentOrThrow();
            return new()
            {
                Labels = league.Labels.ToDictionary(x => x.Key, x => x.Value),
                PenaltyPoints = league.GetPenaltyPoints().ToDictionary(x => x.Key.Id, x => x.Value),
                Rules = league.RankingRules,
            };
        }

        public RankingDto GetRanking(Guid matchdayId)
        {
            var league = _leagueRepository.GetCurrentOrThrow();
            var matchday = league.Matchdays.GetById(matchdayId);

            return ToDto(league.GetRanking(matchday));
        }

        public RankingDto GetRanking(bool live = false)
        {
            var league = _leagueRepository.GetCurrentOrThrow();

            var previousRanking = live ? league.GetRanking() : null;
            var ranking = live ? league.GetLiveRanking() : league.GetRanking();

            return ToDto(ranking, previousRanking);
        }

        public RankingDto GetHomeRanking() => ToDto(_leagueRepository.GetCurrentOrThrow().GetHomeRanking());

        public RankingDto GetAwayRanking() => ToDto(_leagueRepository.GetCurrentOrThrow().GetAwayRanking());

        public int GetNumberOfMatchays(BuildParametersDto dto) => GetMatchdaysAlgorithm(dto).NumberOfMatchdays(_leagueRepository.GetCurrentOrThrow().Teams.Count);

        public int GetNumberOfMatchesByMatchday(BuildParametersDto dto) => GetMatchdaysAlgorithm(dto).NumberOfMatchesByMatchday(_leagueRepository.GetCurrentOrThrow().Teams.Count);

        public void Build(BuildParametersDto dto)
        {
            var league = _leagueRepository.GetCurrentOrThrow();

            _matchdayRepository.Clear(league);

            if (dto.SchedulingParameters is not null)
                _schedulingParametersRepository.Update(league, dto.SchedulingParameters);

            if (dto.MatchFormat is not null)
                _leagueRepository.UpdateMatchFormat(dto.MatchFormat);

            var algorithm = GetMatchdaysAlgorithm(dto);

            MatchdaysBuilder? builder = null;

            switch (dto.BuildDatesParameters)
            {
                case BuildManualParametersDto buildManualParametersDto:
                    builder = new MatchdaysByDatesBuilder().SetDates(buildManualParametersDto.Dates ?? []);
                    break;
                case BuildAutomaticParametersDto buildAutomaticParametersDto:
                    builder = new MatchdaysByDatesBuilder().SetDates(buildAutomaticParametersDto.Dates ?? []);
                    break;
                case BuildAsSoonAsPossibleParametersDto buildAsSoonAsPossibleParametersDto:
                    var builderTemp = new MatchdaysAsSoonAsPossibleBuilder();

                    builderTemp.Rules.AddRange(buildAsSoonAsPossibleParametersDto.Rules ?? []);

                    if (dto.SchedulingParameters is not null)
                    {
                        builderTemp.StartDate = buildAsSoonAsPossibleParametersDto.StartDate.GetValueOrDefault(dto.SchedulingParameters.StartDate.ToUtcDateTime(dto.SchedulingParameters.StartTime)).ToUniversalTime();
                        builderTemp.RestTime = dto.SchedulingParameters.RestTime;
                        builderTemp.RotationTime = dto.SchedulingParameters.RotationTime;
                    }

                    if (dto.MatchFormat is not null)
                        builderTemp.MatchFormat = dto.MatchFormat;

                    builder = builderTemp;
                    break;
                default:
                    break;
            }

            if (builder is not null)
            {
                builder.NamePattern = dto.NamePattern.OrEmpty();
                builder.ShortNamePattern = dto.ShortNamePattern.OrEmpty();
                builder.UseTeamVenues = dto.SchedulingParameters?.UseTeamVenues ?? false;

                var matchdays = builder.Build(league, algorithm);

                matchdays.ForEach(x => _matchdayRepository.Insert(league, x));
            }
        }

        private static IMatchdaysAlgorithm GetMatchdaysAlgorithm(BuildParametersDto buildParameters) => buildParameters.Algorithm switch
        {
            ChampionshipAlgorithm.RoundRobin => new RoundRobinAlgorithm()
            {
                NumberOfMatchesBetweenTeams = buildParameters.MatchesBetweenTeams?.Length ?? 2,
                InvertTeamsByStage = buildParameters.MatchesBetweenTeams ?? []
            },
            ChampionshipAlgorithm.SwissSystem => new SwissSystemAlgorithm()
            {
                NumberOfMatchesByTeams = buildParameters.NumberOfMatchesByTeam,
            },
            _ => throw new InvalidOperationException("Algorithm is unknown")
        };

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
