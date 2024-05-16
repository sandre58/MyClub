// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Application.Converters;
using MyClub.Teamup.Application.Dtos;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Teamup.Application.Services
{
    public class TeamsImportService(IReadService readService)
    {
        public static readonly IEnumerable<ColumnMapping<TeamExportDto, object?>> DefaultColumns =
        [
            new(x => x.Club, nameof(MyClubResources.Club)),
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Category, nameof(MyClubResources.Category), new EnumerationConverter<Category>()),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Stadium, nameof(MyClubResources.Stadium), new CsvStadiumConverter()),
            new(x => x.HomeColor, nameof(MyClubResources.HomeColor)),
            new(x => x.AwayColor, nameof(MyClubResources.AwayColor)),
        ];

        private readonly IReadService _readService = readService;

        public (ICollection<TeamExportDto> Teams, ICollection<Exception> Errors) ExtractTeams(string filename)
            => _readService.CanRead(filename)
                ? (ExtractTeamsFromTmprojFile(filename), Array.Empty<Exception>())
                : ExtractTeamsFromFile(filename);

        private List<TeamExportDto> ExtractTeamsFromTmprojFile(string filename)
        {
            var teams = _readService.ReadTeamsAsync(filename).Result;

            return teams.Select(x =>
            {
                var item = new TeamExportDto
                {
                    HomeColor = x.HomeColor.Value,
                    AwayColor = x.AwayColor.Value,
                    Logo = x.Club.Logo,
                    Club = x.Club.Name,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Category = x.Category,
                    Country = x.Club.Country,
                    Stadium = x.Stadium.Value is not null
                              ? new StadiumDto
                              {
                                  Name = x.Stadium.Value.Name,
                                  Address = x.Stadium.Value.Address,
                                  Ground = x.Stadium.Value.Ground,
                              } : null
                };

                return item;
            }).ToList();
        }

        private static (ICollection<TeamExportDto>, ICollection<Exception>) ExtractTeamsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<TeamExportDto>(DefaultColumns, false, false));

            var teams = reader.GetRecords<TeamExportDto>().ToList();

            var index = 1;
            var convertedTeams = teams.Select(x =>
            {
                var team = new TeamExportDto();

                trySetValue(() => team.Club = x.Club, nameof(TeamExportDto.Club), x.Name);
                trySetValue(() => team.Name = x.Name, nameof(TeamExportDto.Name), x.Name);
                trySetValue(() => team.ShortName = x.ShortName, nameof(TeamExportDto.ShortName), x.ShortName);
                trySetValue(() => team.Country = x.Country, nameof(TeamExportDto.Country), x.Country);
                trySetValue(() => team.Category = x.Category, nameof(TeamExportDto.Category), x.Category);
                trySetValue(() => team.HomeColor = x.HomeColor, nameof(TeamExportDto.HomeColor), x.HomeColor);
                trySetValue(() => team.AwayColor = x.AwayColor, nameof(TeamExportDto.AwayColor), x.AwayColor);
                trySetValue(() => team.Stadium = x.Stadium, nameof(TeamExportDto.Stadium), x.Stadium);

                index++;
                return team;

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
            return (convertedTeams, exceptions);
        }
    }
}
