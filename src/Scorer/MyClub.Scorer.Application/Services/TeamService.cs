// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class TeamService(ITeamRepository repository, IStadiumRepository stadiumRepository) : CrudService<Team, TeamDto, ITeamRepository>(repository)
    {
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        protected override Team CreateEntity(TeamDto dto)
        {
            var entity = new Team(dto.Name.OrEmpty(), dto.ShortName.OrEmpty());
            UpdateEntity(entity, dto);

            return entity;
        }

        protected override void UpdateEntity(Team entity, TeamDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();
            entity.Logo = dto.Logo;
            entity.Country = dto.Country;
            entity.AwayColor = dto.AwayColor;
            entity.HomeColor = dto.HomeColor;
            entity.Stadium = dto.Stadium is null
                ? null
                : dto.Stadium.Id.HasValue
                    ? _stadiumRepository.GetById(dto.Stadium.Id.Value)
                    : _stadiumRepository.Insert(new Stadium(dto.Stadium.Name.OrEmpty(), dto.Stadium.Ground)
                    {
                        Address = dto.Stadium.Address
                    });
        }

        public Team Add(string name) => Save(new TeamDto { Name = name });

        public TeamDto NewTeam()
        {
            var existingTeamNames = GetAll().Select(x => x.Name).ToList();
            var name = MyClubResources.Team.Increment(existingTeamNames, format: " #");
            return new()
            {
                Name = name,
                ShortName = name.GetInitials(),
            };
        }

        public IList<Team> GetSimilarTeams(string name)
            => Repository.GetAll().Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).ToList();

        public bool Remove(Guid id, bool removeStadium)
        {
            if (removeStadium)
            {
                var entity = GetById(id).OrThrow();

                if (entity.Stadium is not null)
                    _stadiumRepository.Remove(entity.Stadium.Id);
            }
            return Remove(id);
        }

        public void Remove(IEnumerable<Guid> ids, bool removeStadium)
        {
            if (removeStadium)
            {
                ids.ForEach(x =>
                {
                    var entity = GetById(x).OrThrow();

                    if (entity.Stadium is not null)
                        _stadiumRepository.Remove(entity.Stadium.Id);
                });
            }

            Remove(ids);
        }

        public void Import(IEnumerable<TeamDto> itemsToAdd, IEnumerable<TeamDto> itemsToUpdate)
        {
            var itemsToSave = itemsToAdd.ToList();

            itemsToUpdate.ForEach(x =>
            {
                var item = GetSimilarTeams(x.Name.OrEmpty()).FirstOrDefault();

                if (item is not null)
                {
                    x.Id = item.Id;
                    itemsToSave.Add(x);
                }
            });

            Save(itemsToSave);
        }
    }
}
