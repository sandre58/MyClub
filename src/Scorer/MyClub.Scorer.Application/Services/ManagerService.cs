// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Services;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class ManagerService(IManagerRepository repository, ITeamRepository teamRepository) : CrudService<Manager, ManagerDto, IManagerRepository>(repository)
    {
        private readonly ITeamRepository _teamRepository = teamRepository;

        protected override Manager CreateEntity(ManagerDto dto)
        {
            var team = _teamRepository.GetById(dto.TeamId) ?? throw new InvalidOperationException($"Team '{dto.TeamId}' not found");

            var entity = Repository.Insert(team, dto.FirstName.OrEmpty(), dto.LastName.OrEmpty());

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Manager entity, ManagerDto dto)
        {
            entity.FirstName = dto.FirstName.OrEmpty();
            entity.LastName = dto.LastName.OrEmpty();
            entity.Country = dto.Country;
            entity.Photo = dto.Photo;
            entity.Gender = dto.Gender;
            entity.LicenseNumber = dto.LicenseNumber;
            entity.Email = dto.Email;
        }

        public ManagerDto New(Guid teamId)
        {
            var team = _teamRepository.GetById(teamId) ?? throw new InvalidOperationException($"Team '{teamId}' not found");

            return new()
            {
                Gender = GenderType.Male,
                Country = team.Country,
            };
        }
    }
}
