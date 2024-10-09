// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Plugins.Scorer.Import.File.Converters;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.IO;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Scorer.Import.File.Providers
{
    internal class TeamsFileProvider(IReadService readService) : ItemsFileProvider<TeamImportDto>
    {
        public static readonly IEnumerable<ColumnMapping<TeamImportDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Country, nameof(MyClubResources.Country)),
            new(x => x.Stadium, nameof(MyClubResources.Stadium), new CsvStadiumConverter()),
            new(x => x.HomeColor, nameof(MyClubResources.HomeColor)),
            new(x => x.AwayColor, nameof(MyClubResources.AwayColor)),
        ];

        private readonly IReadService _readService = readService;

        protected override (IEnumerable<TeamImportDto> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
            => _readService.CanRead(filename)
                ? (ExtractTeamsFromScprojFile(filename), Array.Empty<Exception>())
                : ExtractTeamsFromFile(filename);

        private List<TeamImportDto> ExtractTeamsFromScprojFile(string filename)
        {
            var items = _readService.ReadTeamsAsync(filename).Result;

            return items.Select(x =>
            {
                var item = new TeamImportDto
                {
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Logo = x.Logo,
                    HomeColor = x.HomeColor,
                    AwayColor = x.AwayColor,
                    Country = x.Country?.ToString(),
                    Stadium = x.Stadium is not null
                              ? new StadiumImportDto
                              {
                                  Name = x.Stadium.Name,
                                  Street = x.Stadium.Address?.Street,
                                  PostalCode = x.Stadium.Address?.PostalCode,
                                  City = x.Stadium.Address?.City,
                                  Country = x.Stadium.Address?.Country?.ToString(),
                                  Longitude = x.Stadium.Address?.Longitude,
                                  Latitude = x.Stadium.Address?.Latitude,
                                  Ground = x.Stadium.Ground.ToString(),
                              } : null
                };

                return item;
            }).ToList();
        }

        private static (ICollection<TeamImportDto>, ICollection<Exception>) ExtractTeamsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<TeamImportDto>(DefaultColumns, false, false));

            var teams = reader.GetRecords<TeamImportDto>().ToList();

            var index = 1;
            var convertedTeams = teams.Select(x =>
            {
                var team = new TeamImportDto();

                trySetValue(() => team.Name = x.Name, nameof(TeamImportDto.Name), x.Name);
                trySetValue(() => team.ShortName = x.ShortName, nameof(TeamImportDto.ShortName), x.ShortName);
                trySetValue(() => team.Country = x.Country, nameof(TeamImportDto.Country), x.Country);
                trySetValue(() => team.HomeColor = x.HomeColor, nameof(TeamImportDto.HomeColor), x.HomeColor);
                trySetValue(() => team.AwayColor = x.AwayColor, nameof(TeamImportDto.AwayColor), x.AwayColor);
                trySetValue(() => team.Stadium = x.Stadium, nameof(TeamImportDto.Stadium), x.Stadium);

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
