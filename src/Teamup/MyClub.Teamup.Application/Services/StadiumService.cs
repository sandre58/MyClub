// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Services
{
    public class StadiumService(IStadiumRepository stadiumRepository)
    {
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        public void Update(StadiumDto dto)
        {
            if (dto.Id.HasValue && _stadiumRepository.GetById(dto.Id.Value) is Stadium foundEntity)
                UpdateStadium(foundEntity, dto);
        }

        public Stadium CreateOrUpdate(StadiumDto dto)
            => !dto.Id.HasValue || _stadiumRepository.GetById(dto.Id.Value) is not Stadium foundEntity
                        ? CreateStadium(dto)
                        : UpdateStadium(foundEntity, dto);

        public void AssignStadium(StadiumDto? dto, Action<Stadium?> assignAction)
        {
            var stadium = dto is null ? null : CreateOrUpdate(dto);

            assignAction.Invoke(stadium);
        }

        private static Stadium CreateStadium(StadiumDto dto) => new(dto.Name.OrEmpty(), dto.Ground) { Address = dto.Address };

        private static Stadium UpdateStadium(Stadium entity, StadiumDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.Ground = dto.Ground;
            entity.Address = dto.Address;

            return entity;
        }

        public static StadiumDto NewStadium() => new()
        {
            Name = MyClubResources.NewStadiumName,
            Ground = Ground.Grass
        };

        public IEnumerable<Stadium> GetSimilarStadiums(string name, string? city)
            => _stadiumRepository.GetAll().Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && (x.Address?.City).OrEmpty().Equals(city.OrEmpty(), StringComparison.InvariantCultureIgnoreCase)).ToList();
    }
}
