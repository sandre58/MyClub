// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Humanizer;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Application.Converters;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.Utilities.Geography;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Application.Services
{
    public class PlayersImportService(IReadService readService)
    {
        public static readonly IEnumerable<ColumnMapping<SquadPlayerExportDto, object?>> DefaultColumns =
        [
            new(x => x.LastName, nameof(MyClubResources.Name)),
            new(x => x.FirstName, nameof(MyClubResources.FirstName)),
            new(x => x.Birthdate, nameof(MyClubResources.Birthdate)),
            new(x => x.PlaceOfBirth, nameof(MyClubResources.PlaceOfBirth)),
            new(x => x.Country, nameof(MyClubResources.Nationality), new EnumerationConverter<Country>()),
            new(x => x.Gender, nameof(MyClubResources.Gender), new EnumConverter<GenderType>()),
            new(x => x.Number, nameof(MyClubResources.Number)),
            new(x => x.Positions, nameof(MyClubResources.Positions), new CsvPositionsConverter()),
            new(x => x.Category, nameof(MyClubResources.Category), new EnumerationConverter<Category>()),
            new(x => x.LicenseNumber, nameof(MyClubResources.LicenseNumber)),
            new(x => x.LicenseState, nameof(MyClubResources.LicenseState), new EnumConverter<LicenseState>()),
            new(x => x.IsMutation, nameof(MyClubResources.IsMutation)),
            new(x => x.FromDate, nameof(MyClubResources.InClubFromDate)),
            new(x => x.Laterality, nameof(MyClubResources.Laterality), new EnumConverter<Laterality>()),
            new(x => x.Height, nameof(MyClubResources.Height)),
            new(x => x.Weight, nameof(MyClubResources.Weight)),
            new(x => x.Size, nameof(MyClubResources.Size)),
            new(x => x.ShoesSize, nameof(MyClubResources.ShoesSize)),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Emails, nameof(MyClubResources.Emails), new CsvEmailsConverter()),
            new(x => x.Phones, nameof(MyClubResources.Phones), new CsvPhonesConverter()),
            new(x => x.Description, nameof(MyClubResources.Description))
        ];

        private readonly IReadService _readService = readService;

        public (ICollection<SquadPlayerDto> Players, ICollection<Exception> Errors) ExtractPlayers(string filename)
            => _readService.CanRead(filename)
                ? (ExtractPlayersFromRosterFile(filename), Array.Empty<Exception>())
                : ExtractPlayersFromFile(filename);

        private List<SquadPlayerDto> ExtractPlayersFromRosterFile(string filename)
        {
            var players = _readService.ReadSquadPlayersAsync(filename).Result;

            return players.Select(x =>
            {
                var item = new SquadPlayerDto
                {
                    FirstName = x.Player.FirstName,
                    LastName = x.Player.LastName,
                    TeamId = null,
                    Category = x.Category,
                    Photo = x.Player.Photo,
                    Gender = x.Player.Gender,
                    Number = x.Number,
                    FromDate = x.FromDate,
                    LicenseState = x.LicenseState,
                    LicenseNumber = x.Player.LicenseNumber,
                    IsMutation = x.IsMutation,
                    Description = x.Player.Description,
                    Address = x.Player.Address,
                    Birthdate = x.Player.Birthdate,
                    Country = x.Player.Country,
                    Height = x.Player.Height,
                    Laterality = x.Player.Laterality,
                    PlaceOfBirth = x.Player.PlaceOfBirth,
                    ShoesSize = x.ShoesSize,
                    Size = x.Size,
                    Weight = x.Player.Weight,
                    Phones = new List<PhoneDto>(x.Player.Phones.Select(x => new PhoneDto
                    {
                        Value = x.Value,
                        Default = x.Default,
                        Label = x.Label
                    })),
                    Emails = new List<EmailDto>(x.Player.Emails.Select(x => new EmailDto
                    {
                        Value = x.Value,
                        Default = x.Default,
                        Label = x.Label
                    })),
                    Positions = new List<RatedPositionDto>(x.Positions.Select(x => new RatedPositionDto
                    {
                        IsNatural = x.IsNatural,
                        Position = x.Position,
                        Rating = x.Rating
                    })),
                    Injuries = new List<InjuryDto>(x.Player.Injuries.Select(x => new InjuryDto
                    {
                        Category = x.Category,
                        Condition = x.Condition,
                        Date = x.Period.Start,
                        Description = x.Description,
                        EndDate = x.Period.End,
                        Severity = x.Severity,
                        Type = x.Type
                    }))
                };

                return item;
            }).ToList();
        }

        private static (ICollection<SquadPlayerDto>, ICollection<Exception>) ExtractPlayersFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<SquadPlayerExportDto>(DefaultColumns, false, false));

            var players = reader.GetRecords<SquadPlayerExportDto>().ToList();

            var index = 1;
            var convertedPlayers = players.Select(x =>
            {
                var player = new SquadPlayerDto();

                trySetValue(() => player.FirstName = x.FirstName, nameof(Player.FirstName), x.FirstName);
                trySetValue(() => player.LastName = x.LastName, nameof(Player.LastName), x.LastName);
                if (!string.IsNullOrEmpty(x.Street) && !string.IsNullOrEmpty(x.PostalCode) && !string.IsNullOrEmpty(x.City))
                    trySetValue(() => player.Address = new Address(x.Street, x.PostalCode, x.City), nameof(Player.Address), player.Address);
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
                x.Emails?.ForEach(y => trySetValue(() => player.Emails.Add(new EmailDto { Value = y.Value.OrEmpty(), Label = y.Label, Default = y.Default }), nameof(Player.Emails), y.Value));

                player.Phones = [];
                x.Phones?.ForEach(y => trySetValue(() => player.Phones.Add(new PhoneDto { Value = y.Value.OrEmpty(), Label = y.Label, Default = y.Default }), nameof(Player.Phones), y.Value));

                player.Positions = [];
                x.Positions?.ForEach(y => trySetValue(() => player.Positions.Add(new RatedPositionDto { IsNatural = y.IsNatural, Position = y.Position, Rating = y.Rating }), nameof(Player.Positions), y.Position));

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
