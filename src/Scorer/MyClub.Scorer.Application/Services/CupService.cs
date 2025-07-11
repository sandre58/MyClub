// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class CupService(ICupRepository cupRepository, IStadiumRepository stadiumRepository, IParametersRepository parametersRepository)
    {
        private readonly ICupRepository _cupRepository = cupRepository;
        private readonly IParametersRepository _parametersRepository = parametersRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        public MatchFormat GetMatchFormat() => _cupRepository.GetCurrentOrThrow().MatchFormat;

        public SchedulingParameters GetSchedulingParameters() => _cupRepository.GetCurrentOrThrow().SchedulingParameters;

        public IList<Round> Build(BuildParametersDto dto)
        {
            if (dto.BracketParameters is not BuildRoundsParametersDto roundsParametersDto) throw new InvalidOperationException("No rounds parameters provided");

            // Save MatchFormat
            if (dto.MatchFormat is not null)
                _parametersRepository.UpdateMatchFormat(dto.MatchFormat);

            var cup = _cupRepository.GetCurrentOrThrow();
            var rounds = ComputeRounds(cup, roundsParametersDto, _stadiumRepository.GetAll().ToList());

            // Save SchedulingParameters
            if (dto.SchedulingParameters is not null)
                _parametersRepository.UpdateSchedulingParameters(new SchedulingParameters(
                   !dto.AutomaticStartDate ? rounds.SelectMany(x => x.GetAllMatches()).MinOrDefault(x => x.Date.ToDate(), rounds.SelectMany(x => x.Stages).MinOrDefault(x => x.Date.ToDate())) : dto.SchedulingParameters.StartDate,
                   !dto.AutomaticEndDate ? rounds.SelectMany(x => x.GetAllMatches()).MaxOrDefault(x => x.Date.ToDate(), rounds.SelectMany(x => x.Stages).MaxOrDefault(x => x.Date.ToDate())) : dto.SchedulingParameters.EndDate,
                   dto.SchedulingParameters.StartTime,
                   dto.SchedulingParameters.RotationTime,
                   dto.SchedulingParameters.RestTime,
                   dto.SchedulingParameters.UseHomeVenue,
                   dto.SchedulingParameters.AsSoonAsPossible,
                   dto.SchedulingParameters.Interval,
                   dto.SchedulingParameters.ScheduleByStage,
                   dto.SchedulingParameters.AsSoonAsPossibleRules ?? [],
                   dto.SchedulingParameters.DateRules ?? [],
                   dto.SchedulingParameters.TimeRules ?? [],
                   dto.SchedulingParameters.VenueRules ?? []
                   ));

            // Save matchdays
            _cupRepository.Fill(rounds);

            return rounds;
        }

        public static int GetNumberOfRounds(RoundsAlgorithmDto dto) => GetRoundsAlgorithm(dto).NumberOfRounds(dto.NumberOfTeams);

        public static IList<Round> ComputeRounds(Cup cup, BuildRoundsParametersDto dto, ICollection<Stadium> availableStadiums)
        {
            if (dto.AlgorithmParameters is null) throw new InvalidOperationException("No algorithm parameters provided");

            // Build Matchdays
            var roundsScheduler = dto.BuildDatesParameters switch
            {
                BuildManualDatesParametersDto buildManualParametersDto => (IDateScheduler<IMatchesStage>)new ByDatesStageScheduler().SetDates(buildManualParametersDto.Dates ?? []),
                BuildAutomaticDatesParametersDto buildAutomaticParametersDto => new DateRulesStageScheduler(buildAutomaticParametersDto.StartDate.GetValueOrDefault())
                {
                    DefaultTime = buildAutomaticParametersDto.DefaultTime,
                    Interval = buildAutomaticParametersDto.IntervalValue.ToTimeSpan(buildAutomaticParametersDto.IntervalUnit),
                    DateRules = buildAutomaticParametersDto.DateRules ?? [],
                    TimeRules = buildAutomaticParametersDto.TimeRules ?? [],
                },
                BuildAsSoonAsPossibleDatesParametersDto buildAsSoonAsPossibleParametersDto => new AsSoonAsPossibleStageScheduler(buildAsSoonAsPossibleParametersDto.StartDate.GetValueOrDefault(DateTime.Today))
                {
                    Rules = buildAsSoonAsPossibleParametersDto.Rules ?? [],
                    ScheduleVenues = dto.ScheduleVenues && dto.AsSoonAsPossibleVenues,
                    AvailableStadiums = availableStadiums,
                },
                _ => throw new InvalidOperationException("No scheduler found with these parameters"),
            };

            IVenueScheduler? venuesScheduler = null;
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

            var algorithm = GetRoundsAlgorithm(dto.AlgorithmParameters);
            var rounds = new OneLegRoundsBuilder(roundsScheduler, venuesScheduler)
            {
                NamePattern = dto.NamePattern.OrEmpty(),
                ShortNamePattern = dto.ShortNamePattern.OrEmpty(),
                ScheduleVenuesBeforeDates = dto.ScheduleVenuesBeforeDates
            }.Build(cup, algorithm).ToList();

            return rounds;
        }

        private static IRoundsAlgorithm GetRoundsAlgorithm(RoundsAlgorithmDto dto) => dto switch
        {
            ByeTeamDto => new ByeTeamAlgorithm(),
            PreliminaryRoundDto => new PreliminaryRoundAlgorithm(),
            _ => throw new InvalidOperationException("Algorithm is unknown")
        };
    }
}
