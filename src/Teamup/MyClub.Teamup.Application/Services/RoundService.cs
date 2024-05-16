// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Application.Services
{
    public class RoundService(IRoundRepository repository, ICompetitionSeasonRepository competitionRepository, MatchdayService matchdayService) : CrudService<IRound, RoundDto, IRoundRepository>(repository)
    {
        private readonly ICompetitionSeasonRepository _competitionRepository = competitionRepository;
        private readonly MatchdayService _matchdayService = matchdayService;

        protected override IRound CreateEntity(RoundDto dto)
        {
            var parent = _competitionRepository.GetById(dto.ParentId.GetValueOrDefault()).OrThrow().CastIn<CupSeason>();
            IRound round = dto switch
            {
                KnockoutDto knockout => new Knockout(dto.Name.OrEmpty(), dto.ShortName.OrEmpty(), knockout.Date, dto.Rules?.CastIn<CupRules>()),
                GroupStageDto groupStage => new GroupStage(dto.Name.OrEmpty(), dto.ShortName.OrEmpty(), groupStage.StartDate, groupStage.EndDate, groupStage.Rules?.CastIn<ChampionshipRules>()),
                _ => throw new InvalidOperationException("Round type is unknown")
            };
            var entity = Repository.Insert(parent, round);

            UpdateEntity(entity, dto);

            return entity;
        }

        protected override void OnItemAdded(IRound item)
        {
            if (item is GroupStage groupStage)
            {
                var dto = _matchdayService.NewMatchday(item.Id);
                dto.Date = groupStage.Period.Start.AddFluentTimeSpan(groupStage.Rules.MatchTime);
                _matchdayService.Save(dto);
            }
        }

        protected override void UpdateEntity(IRound entity, RoundDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();

            if (dto.Rules is not null)
                entity.Rules = dto.Rules;

            if (dto.TeamIds is not null)
            {
                var parent = _competitionRepository.GetById(dto.ParentId.GetValueOrDefault()).OrThrow().CastIn<CupSeason>();
                entity.SetTeams(parent.Teams.Where(x => dto.TeamIds.Contains(x.Id)));
            }

            if (entity is Knockout knockout && dto is KnockoutDto knockoutDto)
            {
                if (knockoutDto.Date != default)
                    knockout.OriginDate = knockoutDto.Date;

                if (knockoutDto.IsPostponed)
                    knockout.Postpone(knockoutDto.PostponedDate);
                else
                    knockout.Schedule(knockoutDto.PostponedDate);
            }

            if (entity is GroupStage groupStage && dto is GroupStageDto groupStageDto)
            {
                groupStage.Period.SetInterval(groupStageDto.StartDate, groupStageDto.EndDate);

                if (groupStageDto.Groups is not null)
                {
                    var parent = _competitionRepository.GetById(dto.ParentId.GetValueOrDefault()).OrThrow().CastIn<CupSeason>();
                    var newGroups = groupStageDto.Groups.Select(x =>
                    {
                        var group = new Group(groupStage, x.Name.OrEmpty(), x.ShortName.OrEmpty(), x.Id) { Order = x.Order };

                        if (x.TeamIds is not null)
                            group.SetTeams(parent.Teams.Where(y => x.TeamIds.Contains(y.Id)));
                        return group;
                    }).ToList();
                    groupStage.SetGroups(newGroups);
                    entity.SetTeams(groupStage.Groups.SelectMany(x => x.Teams));
                }
            }
        }

        public void Postpone(Guid id, DateTime? postponedDate = null) => Update(id, x => x.IfIs<Knockout>(y => y.Postpone(postponedDate)));

        public void Postpone(IEnumerable<Guid> ids, DateTime? postponedDate = null)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(x => Postpone(x, postponedDate));
        }

        public KnockoutDto NewKnockout(Guid cupId)
        {
            var parent = _competitionRepository.GetById(cupId).OrThrow().CastIn<CupSeason>();
            var roundNames = parent.Rounds.Select(x => x.Name).ToArray();
            var name = MyClubResources.Round.Increment(roundNames, format: " #");
            var shortname = name.GetInitials();
            var item = new KnockoutDto()
            {
                Name = name,
                ShortName = shortname,
                Date = DateTime.Today.AddFluentTimeSpan(parent.Rules.MatchTime),
                Rules = parent.Rules,
            };

            return item;
        }

        public GroupStageDto NewGroupStage(Guid cupId)
        {
            var parent = _competitionRepository.GetById(cupId).OrThrow().CastIn<CupSeason>();
            var roundNames = parent.Rounds.Select(x => x.Name).ToArray();
            var name = MyClubResources.Round.Increment(roundNames, format: " #");
            var shortname = name.GetInitials();
            var item = new GroupStageDto()
            {
                Name = name,
                ShortName = shortname,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(60),
                Rules = LeagueRules.Default,
                Groups = [new()
                {
                    Name = MyClubResources.GroupX.FormatWith(1),
                    ShortName = MyClubResources.GroupX.FormatWith(1).GetInitials(),
                }
                ]
            };

            return item;
        }
    }
}
