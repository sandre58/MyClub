// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class InjuryService(IInjuryRepository repository,
                               ISquadPlayerRepository playerRepository,
                               InjuriesStatisticsRefreshDeferrer injuriesStatisticsManager) : CrudService<Injury, InjuryDto, IInjuryRepository>(repository)
    {
        private readonly ISquadPlayerRepository _playerRepository = playerRepository;
        private readonly InjuriesStatisticsRefreshDeferrer _injuriesStatisticsRefreshDeferrer = injuriesStatisticsManager;

        protected override Injury CreateEntity(InjuryDto dto)
        {
            var entity = Repository.Insert(_playerRepository.GetByPlayerId(dto.PlayerId.GetValueOrDefault()).Player, dto.Date, dto.Condition.OrEmpty(), dto.Severity);

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Injury entity, InjuryDto dto)
        {
            entity.Period.SetInterval(dto.Date, dto.EndDate);
            entity.Category = dto.Category;
            entity.Condition = dto.Condition.OrEmpty();
            entity.Severity = dto.Severity;
            entity.Type = dto.Type;
            entity.Description = dto.Description;
        }

        public static InjuryDto New() => new()
        {
            Date = DateTime.Today
        };

        protected override void OnCollectionChanged() => _injuriesStatisticsRefreshDeferrer.AskRefresh();
    }
}
