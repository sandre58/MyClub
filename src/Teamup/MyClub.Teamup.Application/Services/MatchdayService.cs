// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class MatchdayService(IMatchdayRepository repository, ICompetitionSeasonRepository competitionRepository, MatchService matchService) : CrudService<Matchday, MatchdayDto, IMatchdayRepository>(repository)
    {
        private readonly ICompetitionSeasonRepository _competitionRepository = competitionRepository;
        private readonly MatchService _matchService = matchService;

        protected override Matchday CreateEntity(MatchdayDto dto)
        {
            var parent = _competitionRepository.GetMatchdaysParent(dto.ParentId.GetValueOrDefault());
            var entity = Repository.Insert(parent, dto.Name.OrEmpty(), dto.Date, dto.ShortName);
            if (dto.DuplicatedMatchdayId.HasValue && GetById(dto.DuplicatedMatchdayId.Value) is Matchday matchdaySource)
            {
                var matches = _matchService.Save(matchdaySource.Matches.Select(x => new MatchDto
                {
                    ParentId = entity.Id,
                    HomeTeamId = x.HomeTeam.Id,
                    AwayTeamId = x.AwayTeam.Id,
                    Date = entity.Date,
                    Stadium = x.Stadium is null ? null : new StadiumDto
                    {
                        Id = x.Stadium.Id,
                        Address = x.Stadium.Address,
                        Ground = x.Stadium.Ground,
                        Name = x.Stadium.Name
                    }
                }), false);

                if (dto.InvertTeams)
                    matches.ForEach(x => x.Invert());
            }

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Matchday entity, MatchdayDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();

            if (dto.Date != default)
                entity.OriginDate = dto.Date;

            if (dto.IsPostponed)
                entity.Postpone(dto.PostponedDate);
            else
                entity.Schedule(dto.PostponedDate);
        }

        public void Postpone(Guid id, DateTime? postponedDate = null) => Update(id, x => x.Postpone(postponedDate));

        public void Postpone(IEnumerable<Guid> ids, DateTime? postponedDate = null)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(x => Postpone(x, postponedDate));
        }

        public MatchdayDto NewMatchday(Guid parentId)
        {
            var parent = _competitionRepository.GetMatchdaysParent(parentId);
            var matchdayNames = parent?.Matchdays.Select(x => x.Name).ToArray() ?? [];
            var name = MyClubResources.Matchday.Increment(matchdayNames, format: " #");
            var shortname = name.GetInitials();
            var item = new MatchdayDto()
            {
                Name = name,
                ParentId = parentId,
                ShortName = shortname,
                Date = DateTime.Today.AddFluentTimeSpan(parent?.Rules.MatchTime ?? TimeSpan.Zero),
            };

            return item;
        }
    }
}
