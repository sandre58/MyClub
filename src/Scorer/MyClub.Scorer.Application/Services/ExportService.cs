// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Application.Services
{
    public static class ExportService
    {
        public static async Task ExportAsCsvOrExcelAsync<T>(IEnumerable<T> players, IEnumerable<ColumnMapping<T, object?>> columns, string filename, bool displayTraduction = true)
        {
            using var writer = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new ExcelWriter(filename)
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvWriter(new StreamWriter(filename), CultureInfo.CurrentCulture)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            writer.Context.RegisterClassMap(new DynamicClassMap<T>(columns, true, displayTraduction));
            await writer.WriteRecordsAsync(players).ConfigureAwait(false);
        }
    }
}
