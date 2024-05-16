// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class MatchdayService(IMatchdayRepository repository,
                                 IProjectRepository projectRepository,
                                 MatchService matchService) : CrudService<Matchday, MatchdayDto, IMatchdayRepository>(repository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
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

            if (dto.Date != default)
                entity.OriginDate = dto.Date;

            if (dto.MatchesToDelete is not null)
                _matchService.Remove(dto.MatchesToDelete);

            if (dto.MatchesToAdd is not null)
            {
                dto.MatchesToAdd.ForEach(x => x.ParentId = entity.Id);
                _matchService.Save(dto.MatchesToAdd);
            }

            if (dto.IsPostponed)
                entity.Postpone(dto.PostponedDate, true);
            else
                entity.Schedule(dto.PostponedDate, true);
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
            var time = _projectRepository.GetCurrentOrThrow().Parameters.MatchStartTime;
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
