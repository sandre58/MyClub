// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class LeagueService(ILeagueRepository leagueRepository, IMatchdayRepository matchdayRepository, IStadiumRepository stadiumRepository)
    {
        private readonly ILeagueRepository _leagueRepository = leagueRepository;
        private readonly IMatchdayRepository _matchdayRepository = matchdayRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        public void UpdateRankingRules(RankingRulesDto dto)
        {
            if (dto.Rules is not null)
                _leagueRepository.UpdateRankingRules(dto.Rules);

            if (dto.Labels is not null)
                _leagueRepository.UpdateLabels(dto.Labels);

            if (dto.PenaltyPoints is not null)
                _leagueRepository.UpdatePenaltyPoints(dto.PenaltyPoints);
        }

        public void UpdateMatchFormat(MatchFormat matchFormat) => _leagueRepository.UpdateMatchFormat(matchFormat);

        public void UpdateSchedulingParameters(SchedulingParameters schedulingParameters) => _leagueRepository.UpdateSchedulingParameters(schedulingParameters);

        public MatchFormat GetMatchFormat() => _leagueRepository.GetCurrentOrThrow().MatchFormat;

        public SchedulingParameters GetSchedulingParameters() => _leagueRepository.GetCurrentOrThrow().SchedulingParameters;

        public bool HasMatches() => _leagueRepository.GetCurrentOrThrow().GetAllMatches().Any();

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

        public IList<Matchday> Build(BuildParametersDto dto)
        {
            if (dto.BracketParameters is not BuildMatchdaysParametersDto matchdaysParametersDto) throw new InvalidOperationException("No matchdays parameters provided");

            // Save MatchFormat
            if (dto.MatchFormat is not null)
                _leagueRepository.UpdateMatchFormat(dto.MatchFormat);

            var league = _leagueRepository.GetCurrentOrThrow();
            var matchdays = ComputeMatchdays(league, matchdaysParametersDto, _stadiumRepository.GetAll().ToList());

            // Save SchedulingParameters
            if (dto.SchedulingParameters is not null)
                _leagueRepository.UpdateSchedulingParameters(new SchedulingParameters(
                   !dto.AutomaticStartDate ? matchdays.SelectMany(x => x.Matches).MinOrDefault(x => x.Date.ToDate(), matchdays.MinOrDefault(x => x.Date.ToDate())) : dto.SchedulingParameters.StartDate,
                   !dto.AutomaticEndDate ? matchdays.SelectMany(x => x.Matches).MaxOrDefault(x => x.Date.ToDate(), matchdays.MaxOrDefault(x => x.Date.ToDate())) : dto.SchedulingParameters.EndDate,
                   dto.SchedulingParameters.StartTime,
                   dto.SchedulingParameters.RotationTime,
                   dto.SchedulingParameters.RestTime,
                   dto.SchedulingParameters.UseHomeVenue,
                   dto.SchedulingParameters.AsSoonAsPossible,
                   dto.SchedulingParameters.Interval,
                   dto.SchedulingParameters.ScheduleByParent,
                   dto.SchedulingParameters.AsSoonAsPossibleRules ?? [],
                   dto.SchedulingParameters.DateRules ?? [],
                   dto.SchedulingParameters.TimeRules ?? [],
                   dto.SchedulingParameters.VenueRules ?? []
                   ));

            // Save matchdays
            _matchdayRepository.Clear(league);
            matchdays.ForEach(x => _matchdayRepository.Insert(league, x));

            return matchdays;
        }

        public static int GetNumberOfMatchays(BuildAlgorithmParametersDto dto) => GetMatchdaysAlgorithm(dto).NumberOfMatchdays(dto.NumberOfTeams);

        public static int GetNumberOfMatchesByMatchday(BuildAlgorithmParametersDto dto) => GetMatchdaysAlgorithm(dto).NumberOfMatchesByMatchday(dto.NumberOfTeams);

        public static IList<Matchday> ComputeMatchdays(League league, BuildMatchdaysParametersDto dto, ICollection<Stadium> availableStadiums)
        {
            if (dto.AlgorithmParameters is null) throw new InvalidOperationException("No algorithm parameters provided");

            // Build Matchdays
            var matchdaysScheduler = dto.BuildDatesParameters switch
            {
                BuildManualDatesParametersDto buildManualParametersDto => (IScheduler<Matchday>)new ByDatesScheduler<Matchday>().SetDates(buildManualParametersDto.Dates ?? []),
                BuildAutomaticDatesParametersDto buildAutomaticParametersDto => new DateRulesScheduler<Matchday>()
                {
                    DefaultTime = buildAutomaticParametersDto.DefaultTime,
                    Interval = buildAutomaticParametersDto.IntervalValue.ToTimeSpan(buildAutomaticParametersDto.IntervalUnit),
                    DateRules = buildAutomaticParametersDto.DateRules ?? [],
                    TimeRules = buildAutomaticParametersDto.TimeRules ?? [],
                    StartDate = buildAutomaticParametersDto.StartDate.GetValueOrDefault()
                },
                BuildAsSoonAsPossibleDatesParametersDto buildAsSoonAsPossibleParametersDto => new AsSoonAsPossibleScheduler<Matchday>()
                {
                    Rules = buildAsSoonAsPossibleParametersDto.Rules ?? [],
                    ScheduleVenues = dto.ScheduleVenues && dto.AsSoonAsPossibleVenues,
                    AvailableStadiums = availableStadiums,
                    StartDate = buildAsSoonAsPossibleParametersDto.StartDate.GetValueOrDefault(DateTime.Today)
                },
                _ => throw new InvalidOperationException("No scheduler found with these parameters"),
            };

            IMatchesScheduler? venuesScheduler = null;
            if (dto.ScheduleVenues)
            {
                if (dto.UseHomeVenue)
                    venuesScheduler = new HomeTeamVenueMatchesScheduler();
                else if (dto.VenueRules?.Count != 0)
                    venuesScheduler = new VenueRulesMatchesScheduler(availableStadiums)
                    {
                        Rules = [.. dto.VenueRules],
                    };
            }

            var algorithm = GetMatchdaysAlgorithm(dto.AlgorithmParameters);
            var matchdays = new MatchdaysBuilder(matchdaysScheduler, venuesScheduler)
            {
                NamePattern = dto.NamePattern.OrEmpty(),
                ShortNamePattern = dto.ShortNamePattern.OrEmpty(),
                ScheduleVenuesBeforeDates = dto.ScheduleVenuesBeforeDates
            }.Build(league, algorithm).ToList();

            return matchdays;
        }

        private static IMatchdaysAlgorithm GetMatchdaysAlgorithm(BuildAlgorithmParametersDto dto) => dto switch
        {
            RoundRobinParametersDto roundRobinParametersDto => new RoundRobinAlgorithm()
            {
                NumberOfMatchesBetweenTeams = roundRobinParametersDto.MatchesBetweenTeams?.Length ?? 2,
                InvertTeamsByStage = roundRobinParametersDto.MatchesBetweenTeams ?? []
            },
            SwissSystemParametersDto swissSystemParametersDto => new SwissSystemAlgorithm()
            {
                NumberOfMatchesByTeams = swissSystemParametersDto.NumberOfMatchesByTeam,
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
