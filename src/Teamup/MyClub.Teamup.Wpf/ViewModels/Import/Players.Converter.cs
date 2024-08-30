// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Converters;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayerImportableConverter(PlayerService playerService) : IConverter<PlayerImportDto, PlayerImportableViewModel>
    {
        private readonly PlayerService _playerService = playerService;

        public PlayerImportableViewModel Convert(PlayerImportDto dto)
        {
            var player = new PlayerImportableViewModel(dto.LastName.OrEmpty(), dto.FirstName.OrEmpty(), _playerService, _playerService.GetSimilarPlayers(dto.FirstName.OrEmpty(), dto.LastName.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Photo = dto.Photo,
                Gender = dto.Gender.OrEmpty().DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault),
                Category = dto.Category.OrEmpty().DehumanizeTo<Category>(OnNoMatch.ReturnsDefault),
                FromDate = dto.FromDate,
                Birthdate = dto.Birthdate,
                PlaceOfBirth = dto.PlaceOfBirth,
                Country = dto.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault),
                LicenseNumber = dto.LicenseNumber,
                Description = dto.Description,
                Laterality = dto.Laterality.OrEmpty().DehumanizeTo<Laterality>(OnNoMatch.ReturnsDefault),
                Size = dto.Size,
                City = dto.City,
                PostalCode = dto.PostalCode,
                Address = dto.Street,
                IsMutation = dto.IsMutation,
                LicenseState = dto.LicenseState.OrEmpty().DehumanizeTo<LicenseState>(OnNoMatch.ReturnsDefault),
            };
            player.Height.Value = dto.Height;
            player.Weight.Value = dto.Weight;
            player.Number.Value = dto.Number;
            player.ShoesSize.Value = dto.ShoesSize;

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

        public PlayerImportDto ConvertBack(PlayerImportableViewModel item) => throw new NotImplementedException();
    }
}
