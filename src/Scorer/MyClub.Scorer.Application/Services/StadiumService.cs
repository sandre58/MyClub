// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class StadiumService(IStadiumRepository repository, AvailibilityCheckingService matchValidationService) : CrudService<Stadium, StadiumDto, IStadiumRepository>(repository)
    {
        private readonly AvailibilityCheckingService _matchValidationService = matchValidationService;

        protected override Stadium CreateEntity(StadiumDto dto) => new(dto.Name.OrEmpty(), dto.Ground) { Address = dto.Address };

        protected override void UpdateEntity(Stadium entity, StadiumDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.Ground = dto.Ground;
            entity.Address = dto.Address;
        }

        public static StadiumDto NewStadium() => new()
        {
            Name = MyClubResources.NewStadiumName,
            Ground = Ground.Grass
        };
        public IEnumerable<Stadium> GetSimilarStadiums(string name, string? city)
            => Repository.GetAll().Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && (x.Address?.City).OrEmpty().Equals(city.OrEmpty(), StringComparison.InvariantCultureIgnoreCase)).ToList();

        public void Import(IEnumerable<StadiumDto> itemsToAdd, IEnumerable<StadiumDto> itemsToUpdate)
        {
            var itemsToSave = itemsToAdd.ToList();

            itemsToUpdate.ForEach(x =>
            {
                var item = GetSimilarStadiums(x.Name.OrEmpty(), x.Address?.City).FirstOrDefault();

                if (item is not null)
                {
                    x.Id = item.Id;
                    itemsToSave.Add(x);
                }
            });

            Save(itemsToSave);
        }

        protected override void OnCollectionChanged() => _matchValidationService.CheckAllConflicts();
    }
}
