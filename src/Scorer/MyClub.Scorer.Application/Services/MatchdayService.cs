// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Application.Services
{
    public class MatchdayService(IMatchdayRepository repository,
                                 IProjectRepository projectRepository,
                                 IStadiumRepository stadiumRepository,
                                 MatchService matchService) : CrudService<Matchday, MatchdayDto, IMatchdayRepository>(repository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;
        private readonly MatchService _matchService = matchService;

        protected override Matchday CreateEntity(MatchdayDto dto)
        {
            var parent = _projectRepository.GetCompetition().GetAllMatchdaysProviders().GetByIdOrDefault(dto.ParentId ?? _projectRepository.GetCompetition().Id) ?? throw new InvalidOperationException($"Matchday parent '{dto.ParentId}' not found");

            var entity = Repository.Insert(parent, dto.Date, dto.Name.OrEmpty(), dto.ShortName);

            UpdateEntity(entity, dto);

            return entity;
        }

        protected override void UpdateEntity(Matchday entity, MatchdayDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();

            if (dto.MatchesToDelete is not null)
                _matchService.Remove(dto.MatchesToDelete);

            if (dto.MatchesToAdd is not null)
            {
                dto.MatchesToAdd.ForEach(x => x.ParentId = entity.Id);
                _matchService.Save(dto.MatchesToAdd);
            }

            var schedulingParameters = entity.ProvideSchedulingParameters();
            if (dto.ScheduleAutomatic)
            {
                var matchdays = GetAll().ToList();
                var scheduledMatchdays = matchdays.Except([entity]).ToList();
                var startDate = scheduledMatchdays.Where(x => x.Date < dto.Date).MaxOrDefault(x => x.Date);
                schedulingParameters.Schedule([entity], startDate != DateTime.MinValue ? startDate.AddMinutes(1) : schedulingParameters.Start(), scheduledMatchdays);
            }
            else
            {
                if (dto.IsPostponed || dto.PostponedDate.HasValue)
                    entity.Postpone(dto.PostponedDate, true);
                else if (dto.Date != default)
                    entity.ScheduleWithMatches(dto.Date);
            }

            if (dto.ScheduleStadiumsAutomatic)
            {
                var matchdays = GetAll().ToList();
                var matches = entity.Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList();
                var scheduledMatches = GetAll().Where(x => x.Date < dto.Date).SelectMany(x => x.Matches).Except(matches).ToList();
                schedulingParameters.ScheduleVenues(matches, _stadiumRepository.GetAll().ToList(), scheduledMatches);
            }
        }

        public virtual IList<MatchdayDto> New(NewMatchdaysDto dto)
        {
            var parent = _projectRepository.GetCompetition().GetAllMatchdaysProviders().GetByIdOrDefault(dto.ParentId ?? _projectRepository.GetCompetition().Id) ?? throw new InvalidOperationException($"Matchday parent '{dto.ParentId}' not found");
            var currentMatchdays = parent.Matchdays.OrderBy(x => x.Date).ToList();

            // Build Matchdays
            var countMatchdays = dto.DatesParameters switch
            {
                AddMatchdaysManualDatesParametersDto manualParametersDto => manualParametersDto.Dates?.Count ?? 0,
                AddMatchdaysAutomaticDatesParametersDto automaticParametersDto => automaticParametersDto.EndDate.HasValue
                                                                                    ? computeNumberOfMatchdays(new DatePeriod(automaticParametersDto.StartDate, automaticParametersDto.EndDate.Value), automaticParametersDto.DateRules ?? [])
                                                                                    : automaticParametersDto.Number ?? 0,
                _ => throw new InvalidOperationException("No scheduler found with these parameters"),
            };

            if (countMatchdays == 0) return [];

            // Temporary date, name and shortName
            var matchdays = countMatchdays.Range().Select(x => new Matchday(parent, DateTime.Today, MyClubResources.MatchdayNamePattern, MyClubResources.MatchdayShortNamePattern)).ToList();

            // Add Matches
            if (dto.StartDuplicatedMatchday.HasValue)
            {
                var startIndex = currentMatchdays.IndexOf(currentMatchdays.GetById(dto.StartDuplicatedMatchday.Value));

                if (startIndex > -1)
                {
                    matchdays.ForEach((x, y) =>
                    {
                        var duplicatedMatchday = currentMatchdays.GetByIndex(startIndex + y);

                        duplicatedMatchday?.Matches.OrderBy(z => z.Date).ForEach(z => x.AddMatch(x.Date, dto.InvertTeams ? z.AwayTeam : z.HomeTeam, dto.InvertTeams ? z.HomeTeam : z.AwayTeam));
                    });
                }
            }

            // Schedule dates
            var schedulingParameters = parent.ProvideSchedulingParameters();
            var stadiums = _stadiumRepository.GetAll().ToList();
            var matchdaysScheduler = dto.DatesParameters switch
            {
                AddMatchdaysManualDatesParametersDto manualParametersDto => (IScheduler<Matchday>)new ByDatesScheduler<Matchday>().SetDates(manualParametersDto.Dates?.Select(x => x.At(schedulingParameters.StartTime)).ToList() ?? [], schedulingParameters.StartTime),
                AddMatchdaysAutomaticDatesParametersDto automaticParametersDto => new DateRulesScheduler<Matchday>()
                {
                    DefaultTime = dto.StartTime,
                    Interval = 1.Days(),
                    DateRules = automaticParametersDto.DateRules ?? [],
                    TimeRules = automaticParametersDto.TimeRules ?? [],
                    StartDate = automaticParametersDto.StartDate
                },
                _ => throw new InvalidOperationException("No scheduler found with these parameters"),
            };
            matchdaysScheduler.Schedule(matchdays);

            // Schedule Venue
            if (dto.ScheduleVenues)
            {
                var venuesScheduler = schedulingParameters.GetVenuesScheduler(stadiums, currentMatchdays.SelectMany(x => x.Matches).ToList());
                venuesScheduler?.Schedule(matchdays.SelectMany(x => x.Matches).ToList());
            }

            return matchdays.OrderBy(x => x.Date).Select((x, y) => new MatchdayDto
            {
                Name = StageNamesFactory.ComputePattern(dto.NamePattern.OrEmpty(), dto.StartIndex + y, x.Date),
                ShortName = StageNamesFactory.ComputePattern(dto.ShortNamePattern.OrEmpty(), dto.StartIndex + y, x.Date),
                Date = x.Date,
                MatchesToAdd = x.Matches.Select(z => new MatchDto
                {
                    Date = z.Date,
                    HomeTeamId = z.HomeTeam.Id,
                    AwayTeamId = z.AwayTeam.Id,
                    Stadium = z.Stadium is not null ? new StadiumDto { Id = z.Stadium.Id } : null,
                }).ToList(),
            }).ToList();

            int computeNumberOfMatchdays(DatePeriod period, IEnumerable<IDateSchedulingRule> dateRules)
            {
                var result = 0;
                var date = period.Start;
                DateOnly? previousDate = null;
                while (date < period.End)
                {
                    if (dateRules.All(x => x.Match(date, previousDate)))
                    {
                        result++;
                        previousDate = date;
                    }
                    date = date.AddDays(1);
                }

                return result;
            };
        }

        public void Postpone(Guid id, DateTime? postponedDate = null) => Update(id, x => x.Postpone(postponedDate));

        public void Postpone(IEnumerable<Guid> ids, DateTime? postponedDate = null)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(x => Postpone(x, postponedDate));
        }

        public MatchdayDto New(Guid? parentId = null)
        {
            var parent = _projectRepository.GetCompetition().GetAllMatchdaysProviders().GetByIdOrDefault(parentId ?? _projectRepository.GetCompetition().Id) ?? throw new InvalidOperationException($"Matchday parent '{parentId}' not found");

            var name = MyClubResources.Matchday.Increment(parent.Matchdays.Select(x => x.Name), format: " #");
            var time = parent.ProvideSchedulingParameters().StartTime;
            return new()
            {
                Date = DateTime.UtcNow.At(time),
                IsPostponed = false,
                PostponedDate = null,
                ParentId = parentId,
                Name = name,
                ShortName = name.GetInitials(),
            };
        }

        public int Save(MatchdaysDto dto)
        {
            if (dto.Matchdays is null) return 0;

            var parent = _projectRepository.GetCompetition().GetAllMatchdaysProviders().GetByIdOrDefault(dto.ParentId ?? _projectRepository.GetCompetition().Id) ?? throw new InvalidOperationException($"Matchday parent '{dto.ParentId}' not found");

            var scheduledMatchdays = parent.Matchdays.ToList();
            var schedulingParameters = parent.ProvideSchedulingParameters();

            var matchdays = Save(dto.Matchdays);

            if (dto.ScheduleAutomatic)
            {
                var startDate = scheduledMatchdays.Where(x => x.Date < dto.StartDate).MaxOrDefault(x => x.Date);
                schedulingParameters.Schedule(matchdays, startDate, scheduledMatchdays);
            }

            if (dto.ScheduleStadiumsAutomatic)
                schedulingParameters.ScheduleVenues(matchdays.SelectMany(x => x.Matches).ToList(), _stadiumRepository.GetAll().ToList(), scheduledMatchdays.SelectMany(x => x.Matches).ToList());

            return matchdays.Count;
        }
    }
}
