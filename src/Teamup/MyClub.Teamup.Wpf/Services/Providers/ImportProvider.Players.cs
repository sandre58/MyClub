// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Playerup.Wpf.Services.Providers
{
    internal class ImportPlayersProvider : ItemsImportablesProvider<PlayerDto, PlayerImportableViewModel>
    {
        private readonly PlayerService _playerService;
        private readonly IImportPlayersPlugin _importPlayersPlugin;

        public ImportPlayersProvider(IImportPlayersPlugin importPlayersPlugin, PlayerService playerService)
            : base(importPlayersPlugin.ProvideItems)
        {
            _importPlayersPlugin = importPlayersPlugin;
            _playerService = playerService;
        }

        public override PlayerImportableViewModel Convert(PlayerDto dto)
        {
            var player = new PlayerImportableViewModel(dto.LastName.OrEmpty(), dto.FirstName.OrEmpty(), _playerService, _playerService.GetSimilarPlayers(dto.LastName.OrEmpty(), dto.FirstName.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Photo = dto.Photo,
                Gender = dto.Gender.OrEmpty().DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault),
                Category = dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
                FromDate = dto.FromDate,
                Birthdate = dto.Birthdate,
                PlaceOfBirth = dto.PlaceOfBirth,
                Country = dto.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                LicenseNumber = dto.LicenseNumber,
                Description = dto.Description,
                Laterality = dto.Laterality.OrEmpty().DehumanizeTo<Laterality>(OnNoMatch.ReturnsDefault),
                Size = dto.Size,
                City = dto.City,
                PostalCode = dto.PostalCode,
                Address = dto.Street
            };
            player.Height.Value = dto.Height;
            player.Weight.Value = dto.Weight;

            if (dto.Emails is not null)
                player.Emails.AddRange(dto.Emails.Select(y => new Email(y.Value.OrEmpty(), y.Label, y.Default)));

            if (dto.Phones is not null)
                player.Phones.AddRange(dto.Phones.Select(y => new Phone(y.Value.OrEmpty(), y.Label, y.Default)));

            if (dto.Positions is not null)
                player.Positions.AddRange(dto.Positions.Select(y => new RatedPosition(y.Position.OrEmpty().DehumanizeTo<Position>(OnNoMatch.ReturnsDefault), y.Rating.OrEmpty().DehumanizeTo<PositionRating>(OnNoMatch.ReturnsDefault)) { IsNatural = y.IsNatural }));

            if (dto.Injuries is not null)
                player.Injuries.AddRange(dto.Injuries.Select(y => new Injury(y.Date, y.Condition.OrEmpty(), y.Severity.OrEmpty().DehumanizeTo<InjurySeverity>(OnNoMatch.ReturnsDefault), y.EndDate, y.Type.OrEmpty().DehumanizeTo<InjuryType>(OnNoMatch.ReturnsDefault), y.Category.OrEmpty().DehumanizeTo<InjuryCategory>(OnNoMatch.ReturnsDefault)) { Description = y.Description }));

            player.ValidateProperties();
            return player;
        }

        public override bool CanImport() => _importPlayersPlugin.CanImport();
    }
}
