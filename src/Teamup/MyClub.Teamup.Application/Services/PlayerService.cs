// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Application.Extensions;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Services
{
    public class PlayerService(ISquadPlayerRepository repository,
                               IProjectRepository projectRepository,
                               InjuriesStatisticsRefreshDeferrer injuriesStatisticsManager,
                               TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer) : CrudService<SquadPlayer, SquadPlayerDto, ISquadPlayerRepository>(repository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly InjuriesStatisticsRefreshDeferrer _injuriesStatisticsRefreshDeferrer = injuriesStatisticsManager;
        private readonly TrainingStatisticsRefreshDeferrer _trainingStatisticsRefreshDeferrer = trainingStatisticsRefreshDeferrer;

        protected override SquadPlayer CreateEntity(SquadPlayerDto dto)
        {
            var newEntity = new SquadPlayer(new Player(dto.FirstName.OrEmpty(), dto.LastName.OrEmpty()));
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(SquadPlayer entity, SquadPlayerDto dto)
        {
            entity.Player.FirstName = dto.FirstName.OrEmpty();
            entity.Player.LastName = dto.LastName.OrEmpty();
            entity.Team = dto.TeamId.HasValue ? _projectRepository.GetCurrentOrThrow().Club.Teams.GetById(dto.TeamId.Value) : null;
            entity.Category = dto.Category;
            entity.Player.Laterality = dto.Laterality;
            entity.Player.Height = dto.Height;
            entity.Player.Weight = dto.Weight;
            entity.ShoesSize = dto.ShoesSize;
            entity.LicenseState = dto.LicenseState;
            entity.IsMutation = dto.IsMutation;
            entity.Number = dto.Number;
            entity.Player.Birthdate = dto.Birthdate;
            entity.FromDate = dto.FromDate;
            entity.Player.PlaceOfBirth = dto.PlaceOfBirth;
            entity.Player.Country = dto.Country;
            entity.Player.Category = dto.Category;
            entity.Player.Photo = dto.Photo;
            entity.Player.Gender = dto.Gender;
            entity.Player.LicenseNumber = dto.LicenseNumber;
            entity.Player.Description = dto.Description;
            entity.Size = dto.Size;
            entity.Player.Address = dto.Address;

            if (dto.Positions is not null)
                entity.Positions.UpdateFrom(dto.Positions, x => entity.AddPosition(x.Position!, x.Rating, x.IsNatural), x => entity.RemovePosition(x.Position), (x, y) =>
                {
                    x.Rating = y.Rating;
                    x.IsNatural = y.IsNatural;
                });

            if (dto.Injuries is not null)
                entity.Player.Injuries.UpdateFrom(dto.Injuries, x => entity.Player.AddInjury(x.Date, x.Condition!, x.Severity, x.EndDate, x.Type, x.Category, x.Description), x => entity.Player.RemoveInjury(x.Id), (x, y) =>
                {
                    x.Period.SetInterval(y.Date, y.EndDate);
                    x.Category = y.Category;
                    x.Condition = y.Condition.OrEmpty();
                    x.Severity = y.Severity;
                    x.Type = y.Type;
                    x.Description = y.Description;
                });

            if (dto.Emails is not null)
            {
                entity.Player.ClearEmails();
                dto.Emails.ForEach(x => entity.Player.AddEmail(x.Value!, x.Label, x.Default));
            }

            if (dto.Phones is not null)
            {
                entity.Player.ClearPhones();
                dto.Phones.ForEach(x => entity.Player.AddPhone(x.Value!, x.Label, x.Default));
            }
        }

        public IList<SquadPlayer> Update(IEnumerable<Guid> playerIds, PlayerMultipleDto dto)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                return playerIds.Select(x => Update(x, y =>
                {
                    if (dto.UpdateCategory)
                    {
                        y.Category = dto.Category;
                        y.Player.Category = dto.Category;
                    }

                    if (dto.UpdateLicenseState)
                        y.LicenseState = dto.LicenseState;

                    if (dto.UpdateIsMutation)
                        y.IsMutation = dto.IsMutation;

                    if (dto.UpdateCountry)
                        y.Player.Country = dto.Country;

                    if (dto.UpdateSize)
                        y.Size = dto.Size;

                    if (dto.UpdateShoesSize)
                        y.ShoesSize = dto.ShoesSize;
                })).ToList();
            }
        }

        public IList<SquadPlayer> Import(IEnumerable<SquadPlayerDto> items)
        {
            var isSimilar = new Func<SquadPlayerDto, bool>(x => GetSimilarPlayers(x.FirstName.OrEmpty(), x.LastName.OrEmpty()).Any());
            var players = items.Where(x => !isSimilar(x)).Select(Save).ToList();

            using (CollectionChangedDeferrer.Defer())
            {
                items.Where(isSimilar).ToList().ForEach(y =>
                {
                    var similarPlayer = GetSimilarPlayers(y.FirstName.OrEmpty(), y.LastName.OrEmpty()).FirstOrDefault();

                    if (similarPlayer is not null)
                    {
                        var dto = y;
                        dto.Id = similarPlayer.Id;
                        dto.TeamId = similarPlayer.Team?.Id;
                        dto.LicenseState = similarPlayer.LicenseState;
                        dto.IsMutation = similarPlayer.IsMutation;
                        if (!dto.Number.HasValue)
                            dto.Number = similarPlayer.Number;
                        var newPlayer = Save(dto);
                        players.Add(newPlayer);
                    }
                });
            }

            return players;
        }

        public void Move(Guid playerId, Guid? teamId) => Update(playerId, x => x.Team = teamId.HasValue ? _projectRepository.GetCurrentOrThrow().Club.Teams.GetById(teamId.Value) : null);

        public void Move(IEnumerable<Guid> playerIds, Guid? squadId)
        {
            using (CollectionChangedDeferrer.Defer())
                playerIds.ForEach(x => Move(x, squadId));
        }

        public IList<SquadPlayer> GetSimilarPlayers(string firstName, string lastName)
            => Repository.GetAll().Where(x => x.Player.FirstName.Equals(firstName, StringComparison.InvariantCultureIgnoreCase) && x.Player.LastName.Equals(lastName, StringComparison.InvariantCultureIgnoreCase)).ToList();

        public bool LicenseNumberExists(string licenseNumber) => Repository.GetAll().Any(x => !string.IsNullOrEmpty(x.Player.LicenseNumber) && x.Player.LicenseNumber.Equals(licenseNumber));

        protected override void OnCollectionChanged()
        {
            _injuriesStatisticsRefreshDeferrer.AskRefresh();
            _trainingStatisticsRefreshDeferrer.AskRefresh();
        }

        public SquadPlayerDto NewPlayer() => new()
        {
            Gender = GenderType.Male,
            FromDate = DateTime.Today,
            Country = _projectRepository.GetCurrentOrThrow().Club.Country,
            Laterality = Laterality.RightHander,
            TeamId = _projectRepository.GetCurrentOrThrow().GetMainTeams().FirstOrDefault()?.Id
        };
    }
}
