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
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

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
                schedulingParameters.Schedule([entity], startDate != DateTime.MinValue ? startDate.AddMinutes(1) : schedulingParameters.StartDate, scheduledMatchdays);
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
                Date = DateTime.Today.ToUtcDateTime(time),
                IsPostponed = false,
                PostponedDate = null,
                ParentId = parentId,
                Name = name,
                ShortName = name.GetInitials(),
            };
        }
    }
}
