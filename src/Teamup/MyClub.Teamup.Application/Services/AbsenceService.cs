// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Application.Services
{
    public class AbsenceService(IAbsenceRepository repository, ISquadPlayerRepository playerRepository) : CrudService<Absence, AbsenceDto, IAbsenceRepository>(repository)
    {
        private readonly ISquadPlayerRepository _playerRepository = playerRepository;

        protected override Absence CreateEntity(AbsenceDto dto)
        {
            var entity = Repository.Insert(_playerRepository.GetByPlayerId(dto.PlayerId.GetValueOrDefault()).Player, dto.StartDate, dto.EndDate, dto.Label.OrEmpty());

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Absence entity, AbsenceDto dto)
        {
            entity.Period.SetInterval(dto.StartDate, dto.EndDate);
            entity.Label = dto.Label.OrEmpty();
            entity.Type = dto.Type;
        }

        public static AbsenceDto New(AbsenceType type) => new()
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            Type = type,
            Label = type.GetDefaultLabel()
        };
    }
}
