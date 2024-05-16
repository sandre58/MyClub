// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Contracts;
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
    public class StadiumsImportService(IReadService readService)
    {
        public static readonly IEnumerable<ColumnMapping<StadiumExportDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.Ground, nameof(MyClubResources.Ground), new EnumConverter<Ground>()),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Longitude, nameof(MyClubResources.Longitude)),
            new(x => x.Latitude, nameof(MyClubResources.Latitude)),
        ];

        private readonly IReadService _readService = readService;

        public (ICollection<StadiumExportDto> Stadiums, ICollection<Exception> Errors) ExtractStadiums(string filename)
            => _readService.CanRead(filename)
                ? (ExtractStadiumsFromTmprojFile(filename), Array.Empty<Exception>())
                : ExtractStadiumsFromFile(filename);

        private List<StadiumExportDto> ExtractStadiumsFromTmprojFile(string filename)
        {
            var stadiums = _readService.ReadStadiumsAsync(filename).Result;

            return stadiums.Select(x =>
            {
                var item = new StadiumExportDto
                {
                    Name = x.Name,
                    Ground = x.Ground,
                    Address = x.Address,
                    Street = x.Address?.Street,
                    PostalCode = x.Address?.PostalCode,
                    City = x.Address?.City,
                    Country = x.Address?.Country,
                    Longitude = x.Address?.Longitude,
                    Latitude = x.Address?.Latitude,
                };

                return item;
            }).ToList();
        }

        private static (ICollection<StadiumExportDto>, ICollection<Exception>) ExtractStadiumsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<StadiumExportDto>(DefaultColumns, false, false));

            var stadiums = reader.GetRecords<StadiumExportDto>().ToList();

            var index = 1;
            var convertedStadiums = stadiums.Select(x =>
            {
                var team = new StadiumExportDto();

                trySetValue(() => team.Name = x.Name, nameof(StadiumExportDto.Name), x.Name);
                trySetValue(() => team.Ground = x.Ground, nameof(StadiumExportDto.Ground), x.Ground);
                trySetValue(() => team.Street = x.Street, nameof(StadiumExportDto.Street), x.Street);
                trySetValue(() => team.PostalCode = x.PostalCode, nameof(StadiumExportDto.PostalCode), x.PostalCode);
                trySetValue(() => team.City = x.City, nameof(StadiumExportDto.City), x.City);
                trySetValue(() => team.Country = x.Country, nameof(StadiumExportDto.Country), x.Country);
                trySetValue(() => team.Longitude = x.Longitude, nameof(StadiumExportDto.Longitude), x.Longitude);
                trySetValue(() => team.Latitude = x.Latitude, nameof(StadiumExportDto.Latitude), x.Latitude);

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
            return (convertedStadiums, exceptions);
        }
    }
}
