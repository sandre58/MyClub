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

        public SchedulingParameters Build(BuildParametersDto dto)
        {
            // Save MatchFormat
            if (dto.MatchFormat is not null)
                _leagueRepository.UpdateMatchFormat(dto.MatchFormat);

            // Build Matchdays
            var league = _leagueRepository.GetCurrentOrThrow();
            var matchdaysScheduler = dto.BuildDatesParameters switch
            {
                BuildManualDatesParametersDto buildManualParametersDto => (IScheduler<Matchday>)new ByDatesScheduler<Matchday>().SetDates(buildManualParametersDto.Dates ?? []),
                BuildAutomaticDatesParametersDto buildAutomaticParametersDto => new DateRulesScheduler<Matchday>()
                {
                    DefaultTime = buildAutomaticParametersDto.DefaultTime,
                    Interval = buildAutomaticParametersDto.IntervalValue.ToTimeSpan(buildAutomaticParametersDto.IntervalUnit),
                    DateRules = buildAutomaticParametersDto.DateRules ?? [],
                    TimeRules = buildAutomaticParametersDto.TimeRules ?? [],
                    StartDate = buildAutomaticParametersDto.StartDate.GetValueOrDefault(DateTime.Today).ToUniversalTime()
                },
                BuildAsSoonAsPossibleDatesParametersDto buildAsSoonAsPossibleParametersDto => new AsSoonAsPossibleScheduler<Matchday>()
                {
                    Rules = buildAsSoonAsPossibleParametersDto.Rules ?? [],
                    ScheduleVenues = dto.ScheduleVenues && dto.AsSoonAsPossibleVenues,
                    AvailableStadiums = _stadiumRepository.GetAll().ToList(),
                    StartDate = buildAsSoonAsPossibleParametersDto.StartDate.GetValueOrDefault(DateTime.Today).ToUniversalTime()
                },
                _ => throw new InvalidOperationException("No scheduler found with these parameters"),
            };

            IMatchesScheduler? venuesScheduler = null;
            if (dto.ScheduleVenues)
            {
                if (dto.UseHomeVenue)
                    venuesScheduler = new HomeTeamVenueMatchesScheduler();
                else if (dto.VenueRules?.Count != 0)
                    venuesScheduler = new VenueRulesMatchesScheduler(_stadiumRepository.GetAll().ToList())
                    {
                        Rules = [.. dto.VenueRules],
                    };
            }

            var algorithm = GetMatchdaysAlgorithm(dto);
            var matchdays = new MatchdaysBuilder(matchdaysScheduler, venuesScheduler)
            {
                NamePattern = dto.NamePattern.OrEmpty(),
                ShortNamePattern = dto.ShortNamePattern.OrEmpty(),
                ScheduleVenuesBeforeDates = dto.ScheduleVenuesBeforeDates
            }.Build(league, algorithm);

            // Save SchedulingParameters
            _leagueRepository.UpdateSchedulingParameters(new SchedulingParameters(
               !dto.StartDate.HasValue ? matchdays.SelectMany(x => x.Matches).MinOrDefault(x => x.Date.BeginningOfDay()) : dto.StartDate.Value.ToUniversalTime(),
               !dto.EndDate.HasValue ? matchdays.SelectMany(x => x.Matches).MaxOrDefault(x => x.Date.EndOfDay()) : dto.EndDate.Value.ToUniversalTime(),
               dto.StartTime,
               dto.RotationTime,
               dto.RestTime,
               dto.UseHomeVenue,
               dto.AsSoonAsPossible,
               dto.Interval,
               true,
               dto.AsSoonAsPossibleRules ?? [],
               dto.DateRules ?? [],
               dto.TimeRules ?? [],
               dto.VenueRules ?? []
               ));

            // Save matchdays
            _matchdayRepository.Clear(league);
            matchdays.ForEach(x => _matchdayRepository.Insert(league, x));

            return league.SchedulingParameters;
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
