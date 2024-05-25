// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.Utilities.Sequences;

namespace MyClub.Plugins.Teamup.Import.File.Converters
{
    public class CsvRankingLabelsConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
            => text?.Split(";").Select(x =>
            {
                var label = x.Split(",");
                var min = label.Length > 0 && int.TryParse(label[0], out var v1) ? v1 : 1;
                var max = label.Length > 1 && int.TryParse(label[1], out var v2) ? v2 : 1;
                var name = label.Length > 2 ? label[2] : string.Empty;
                var shortName = label.Length > 3 ? label[3] : string.Empty;
                var color = label.Length > 4 ? label[4] : string.Empty;
                var description = label.Length > 5 ? label[5] : string.Empty;

                return (min, max, new RankLabelImportDto
                {
                    ShortName = shortName,
                    Color = color,
                    Description = description,
                    Name = name
                });
            }).ToDictionary(x => new AcceptableValueRange<int>(x.min, x.max), x => x.Item3);

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
            => value is IDictionary<AcceptableValueRange<int>, RankLabelImportDto> e ? string.Join(";", e.Select(x => string.Join(",", x.Key.Min, x.Key.Max, x.Value.Name, x.Value.ShortName, x.Value.Color, x.Value.Description))) : string.Empty;
    }
}
