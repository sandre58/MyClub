// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Plugins.Teamup.Import.File.Converters;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.IO;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Teamup.Import.File.Providers
{
    internal class PlayersFileProvider(IReadService readService) : ItemsFileProvider<PlayerImportDto>
    {
        public static readonly IEnumerable<ColumnMapping<PlayerImportDto, object?>> DefaultColumns =
        [
            new(x => x.LastName, nameof(MyClubResources.Name)),
            new(x => x.FirstName, nameof(MyClubResources.FirstName)),
            new(x => x.Birthdate, nameof(MyClubResources.Birthdate)),
            new(x => x.PlaceOfBirth, nameof(MyClubResources.PlaceOfBirth)),
            new(x => x.Country, nameof(MyClubResources.Nationality)),
            new(x => x.Gender, nameof(MyClubResources.Gender)),
            new(x => x.Number, nameof(MyClubResources.Number)),
            new(x => x.Positions, nameof(MyClubResources.Positions), new CsvPositionsConverter()),
            new(x => x.Category, nameof(MyClubResources.Category)),
            new(x => x.LicenseNumber, nameof(MyClubResources.LicenseNumber)),
            new(x => x.LicenseState, nameof(MyClubResources.LicenseState)),
            new(x => x.IsMutation, nameof(MyClubResources.IsMutation)),
            new(x => x.FromDate, nameof(MyClubResources.InClubFromDate)),
            new(x => x.Laterality, nameof(MyClubResources.Laterality)),
            new(x => x.Height, nameof(MyClubResources.Height)),
            new(x => x.Weight, nameof(MyClubResources.Weight)),
            new(x => x.Size, nameof(MyClubResources.Size)),
            new(x => x.ShoesSize, nameof(MyClubResources.ShoesSize)),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Emails, nameof(MyClubResources.Emails), new CsvContactsConverter()),
            new(x => x.Phones, nameof(MyClubResources.Phones), new CsvContactsConverter()),
            new(x => x.Description, nameof(MyClubResources.Description))
        ];

        private readonly IReadService _readService = readService;

        protected override (IEnumerable<PlayerImportDto> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
            => _readService.CanRead(filename)
                ? (ExtractPlayersFromRosterFile(filename), Array.Empty<Exception>())
                : ExtractPlayersFromFile(filename);

        private List<PlayerImportDto> ExtractPlayersFromRosterFile(string filename)
        {
            var players = _readService.ReadSquadPlayersAsync(filename).Result;

            return players.Select(x =>
            {
                var item = new PlayerImportDto
                {
                    LastName = x.Player.LastName,
                    FirstName = x.Player.FirstName,
                    Category = x.Category?.Name,
                    Street = x.Player.Address?.Street,
                    City = x.Player.Address?.City,
                    Latitude = x.Player.Address?.Latitude,
                    Longitude = x.Player.Address?.Longitude,
                    PostalCode = x.Player.Address?.PostalCode,
                    AddressCountry = x.Player.Address?.Country?.Name,
                    Birthdate = x.Player.Birthdate,
                    Country = x.Player.Country?.Name,
                    Description = x.Player.Description,
                    Gender = x.Player.Gender.ToString(),
                    Photo = x.Player.Photo,
                    PlaceOfBirth = x.Player.PlaceOfBirth,
                    Laterality = x.Player.Laterality.ToString(),
                    LicenseNumber = x.Player.LicenseNumber,
                    Height = x.Player.Height,
                    Weight = x.Player.Weight,
                    Size = x.Size,
                    FromDate = x.FromDate,
                    IsMutation = x.IsMutation,
                    Number = x.Number,
                    LicenseState = x.LicenseState.ToString(),
                    ShoesSize = x.ShoesSize,
                    Emails = x.Player.Emails?.Select(y => new ContactImportDto
                    {
                        Default = y.Default,
                        Label = y.Label,
                        Value = y.Value,
                    }).ToList(),
                    Injuries = x.Player.Injuries?.Select(y => new InjuryImportDto()
                    {
                        Category = y.Category.ToString(),
                        Severity = y.Severity.ToString(),
                        Type = y.Type.ToString(),
                        Condition = y.Condition,
                        Date = y.Period.Start,
                        Description = y.Description,
                        EndDate = y.Period.End,
                    }).ToList(),
                    Phones = x.Player.Phones?.Select(y => new ContactImportDto
                    {
                        Default = y.Default,
                        Label = y.Label,
                        Value = y.Value,
                    }).ToList(),
                    Positions = x.Positions?.Select(y => new RatedPositionImportDto()
                    {
                        IsNatural = y.IsNatural,
                        Position = y.Position.Name,
                        Rating = y.Rating.ToString()
                    }).ToList(),
                };

                return item;
            }).ToList();
        }

        private static (ICollection<PlayerImportDto>, ICollection<Exception>) ExtractPlayersFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<PlayerImportDto>(DefaultColumns, false, false));

            var players = reader.GetRecords<PlayerImportDto>().ToList();

            var index = 1;
            var convertedPlayers = players.Select(x =>
            {
                var player = new PlayerImportDto();

                trySetValue(() => player.FirstName = x.FirstName, nameof(Player.FirstName), x.FirstName);
                trySetValue(() => player.LastName = x.LastName, nameof(Player.LastName), x.LastName);
                trySetValue(() => player.Street = x.Street, nameof(Address.Street), x.Street);
                trySetValue(() => player.PostalCode = x.PostalCode, nameof(Address.PostalCode), x.PostalCode);
                trySetValue(() => player.City = x.City, nameof(Address.City), x.City);
                trySetValue(() => player.Birthdate = x.Birthdate, nameof(Player.Birthdate), x.Birthdate);
                trySetValue(() => player.Country = x.Country, nameof(Player.Country), x.Country);
                trySetValue(() => player.Description = x.Description, nameof(Player.Description), x.Description);
                trySetValue(() => player.FromDate = x.FromDate, nameof(SquadPlayer.FromDate), x.FromDate);
                trySetValue(() => player.Gender = x.Gender, nameof(Player.Gender), x.Gender);
                trySetValue(() => player.IsMutation = x.IsMutation, nameof(SquadPlayer.IsMutation), x.IsMutation);
                trySetValue(() => player.Laterality = x.Laterality, nameof(Player.Laterality), x.Laterality);
                trySetValue(() => player.Category = x.Category, nameof(SquadPlayer.Category), x.Category);
                trySetValue(() => player.LicenseNumber = x.LicenseNumber, nameof(Player.LicenseNumber), x.LicenseNumber);
                trySetValue(() => player.LicenseState = x.LicenseState, nameof(SquadPlayer.LicenseState), x.LicenseState);
                trySetValue(() => player.Photo = x.Photo, nameof(Player.Photo), x.Photo);
                trySetValue(() => player.PlaceOfBirth = x.PlaceOfBirth, nameof(Player.PlaceOfBirth), x.PlaceOfBirth);
                trySetValue(() => player.Size = x.Size, nameof(SquadPlayer.Size), x.Size);
                trySetValue(() => player.Number = x.Number, nameof(SquadPlayer.Number), x.Number);
                trySetValue(() => player.Height = x.Height, nameof(Player.Height), x.Height);
                trySetValue(() => player.Weight = x.Weight, nameof(Player.Weight), x.Weight);
                trySetValue(() => player.ShoesSize = x.ShoesSize, nameof(SquadPlayer.ShoesSize), x.ShoesSize);

                player.Emails = [];
                x.Emails?.ForEach(y => trySetValue(() => player.Emails.Add(new ContactImportDto { Value = y.Value.OrEmpty(), Label = y.Label, Default = y.Default }), nameof(Player.Emails), y.Value));

                player.Phones = [];
                x.Phones?.ForEach(y => trySetValue(() => player.Phones.Add(new ContactImportDto { Value = y.Value.OrEmpty(), Label = y.Label, Default = y.Default }), nameof(Player.Phones), y.Value));

                player.Positions = [];
                x.Positions?.ForEach(y => trySetValue(() => player.Positions.Add(new RatedPositionImportDto { IsNatural = y.IsNatural, Position = y.Position, Rating = y.Rating }), nameof(Player.Positions), y.Position));

                index++;
                return player;

                void trySetValue<T>(Action action, string columnHeader, T value)
                {
                    try
                    {
                        action();
                    }
                    catch (TranslatableException e)
                    {
                        exceptions?.Add(new ImportValueException(index, columnHeader, value, e.Parameters is not null ? e.ResourceKey.Translate()?.FormatWith(e.Parameters) : e.ResourceKey.Translate(), e));
                    }
                    catch (Exception e)
                    {
                        exceptions?.Add(new ImportValueException(index, columnHeader, value, e.Message, e));
                    }
                }

            }).ToList();
            return (convertedPlayers, exceptions);
        }
    }
}
