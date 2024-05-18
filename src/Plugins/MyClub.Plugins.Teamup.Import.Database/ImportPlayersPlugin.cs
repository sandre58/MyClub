// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Teamup.Import.Database;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportPlayersPlugin : ImportDatabaseItemsPlugin<PlayerDto>, IImportPlayersPlugin
    {
        public override IEnumerable<PlayerDto> LoadItems(IUnitOfWork unitOfWork)
            => unitOfWork.PlayerRepository.GetAll().Select(x => new PlayerDto
            {
                LastName = x.LastName,
                FirstName = x.FirstName,
                Category = x.Category,
                Street = x.Street,
                City = x.City,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                PostalCode = x.PostalCode,
                AddressCountry = x.AddressCountry,
                Birthdate = x.Birthdate,
                Country = x.Country,
                Description = x.Description,
                Gender = x.Gender,
                Photo = x.Photo,
                PlaceOfBirth = x.PlaceOfBirth,
                Laterality = x.Laterality,
                LicenseNumber = x.LicenseNumber,
                Height = x.Height,
                Weight = x.Weight,
                Size = x.Size,
                FromDate = null,
                Emails = x.Emails != null ? x.Emails.Select(y => new ContactDto
                {
                    Default = y.Default,
                    Label = y.Label,
                    Value = y.Value,
                }).ToList() : null,
                Injuries = x.Injuries != null ? x.Injuries.Select(y => new InjuryDto()
                {
                    Category = y.Category,
                    Severity = y.Severity,
                    Type = y.Type,
                    Condition = y.Condition,
                    Date = y.StartDate,
                    Description = y.Description,
                    EndDate = y.EndDate,
                }).ToList() : null,
                Phones = x.Phones != null ? x.Phones.Select(y => new ContactDto
                {
                    Default = y.Default,
                    Label = y.Label,
                    Value = y.Value,
                }).ToList() : null,
                Positions = x.Positions != null ? x.Positions.Select(y => new RatedPositionDto()
                {
                    IsNatural = y.IsNatural,
                    Position = y.Position,
                    Rating = y.Rating
                }).ToList() : null,
            });
    }
}
