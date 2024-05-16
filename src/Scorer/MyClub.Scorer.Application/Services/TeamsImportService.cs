// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Application.Converters;
using MyClub.Scorer.Application.Dtos;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Application.Services
{
    public class TeamsImportService(IReadService readService)
    {
        public static readonly IEnumerable<ColumnMapping<TeamDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Stadium, nameof(MyClubResources.Stadium), new CsvStadiumConverter()),
            new(x => x.HomeColor, nameof(MyClubResources.HomeColor)),
            new(x => x.AwayColor, nameof(MyClubResources.AwayColor)),
        ];

        private readonly IReadService _readService = readService;

        public (ICollection<TeamDto> Teams, ICollection<Exception> Errors) ExtractTeams(string filename)
            => _readService.CanRead(filename)
                ? (ExtractTeamsFromTmprojFile(filename), Array.Empty<Exception>())
                : ExtractTeamsFromFile(filename);

        private List<TeamDto> ExtractTeamsFromTmprojFile(string filename)
        {
            var teams = _readService.ReadTeamsAsync(filename).Result;

            return teams.Select(x =>
            {
                var item = new TeamDto
                {
                    HomeColor = x.HomeColor,
                    AwayColor = x.AwayColor,
                    Logo = x.Logo,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Country = x.Country,
                    Stadium = x.Stadium is not null
                              ? new StadiumDto
                              {
                                  Name = x.Stadium.Name,
                                  Address = x.Stadium.Address,
                                  Ground = x.Stadium.Ground,
                              } : null
                };

                return item;
            }).ToList();
        }

        private static (ICollection<TeamDto>, ICollection<Exception>) ExtractTeamsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<TeamDto>(DefaultColumns, false, false));

            var teams = reader.GetRecords<TeamDto>().ToList();

            var index = 1;
            var convertedTeams = teams.Select(x =>
            {
                var team = new TeamDto();

                trySetValue(() => team.Name = x.Name, nameof(TeamDto.Name), x.Name);
                trySetValue(() => team.ShortName = x.ShortName, nameof(TeamDto.ShortName), x.ShortName);
                trySetValue(() => team.Country = x.Country, nameof(TeamDto.Country), x.Country);
                trySetValue(() => team.HomeColor = x.HomeColor, nameof(TeamDto.HomeColor), x.HomeColor);
                trySetValue(() => team.AwayColor = x.AwayColor, nameof(TeamDto.AwayColor), x.AwayColor);
                trySetValue(() => team.Stadium = x.Stadium, nameof(TeamDto.Stadium), x.Stadium);

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
