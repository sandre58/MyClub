// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
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
    internal class StadiumsFileProvider(IReadService readService) : ItemsFileProvider<StadiumImportDto>
    {
        public static readonly IEnumerable<ColumnMapping<StadiumImportDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.Ground, nameof(MyClubResources.Ground)),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Country, nameof(MyClubResources.Country)),
            new(x => x.Longitude, nameof(MyClubResources.Longitude)),
            new(x => x.Latitude, nameof(MyClubResources.Latitude)),
        ];

        private readonly IReadService _readService = readService;

        protected override (IEnumerable<StadiumImportDto> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
            => _readService.CanRead(filename)
                ? (ExtractStadiumsFromScprojFile(filename), Array.Empty<Exception>())
                : ExtractStadiumsFromFile(filename);

        private List<StadiumImportDto> ExtractStadiumsFromScprojFile(string filename)
        {
            var items = _readService.ReadStadiumsAsync(filename).Result;

            return items.Select(x =>
            {
                var item = new StadiumImportDto
                {
                    Name = x.Name,
                    City = x.Address?.City,
                    Ground = x.Ground.ToString(),
                    Latitude = x.Address?.Latitude,
                    Longitude = x.Address?.Longitude,
                    PostalCode = x.Address?.PostalCode,
                    Street = x.Address?.Street
                };

                return item;
            }).ToList();
        }

        private static (ICollection<StadiumImportDto>, ICollection<Exception>) ExtractStadiumsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<StadiumImportDto>(DefaultColumns, false, false));

            var stadiums = reader.GetRecords<StadiumImportDto>().ToList();

            var index = 1;
            var convertedStadiums = stadiums.Select(x =>
            {
                var stadium = new StadiumImportDto();

                trySetValue(() => stadium.Name = x.Name, nameof(StadiumImportDto.Name), x.Name);
                trySetValue(() => stadium.Street = x.Street, nameof(StadiumImportDto.Street), x.Street);
                trySetValue(() => stadium.City = x.City, nameof(StadiumImportDto.City), x.City);
                trySetValue(() => stadium.PostalCode = x.PostalCode, nameof(StadiumImportDto.PostalCode), x.PostalCode);
                trySetValue(() => stadium.Country = x.Country, nameof(StadiumImportDto.Country), x.Country);
                trySetValue(() => stadium.Latitude = x.Latitude, nameof(StadiumImportDto.Latitude), x.Latitude);
                trySetValue(() => stadium.Longitude = x.Longitude, nameof(StadiumImportDto.Longitude), x.Longitude);

                index++;
                return stadium;

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
