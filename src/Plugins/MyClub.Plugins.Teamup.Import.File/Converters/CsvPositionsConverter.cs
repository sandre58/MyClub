// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Domain.Enums;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.Humanizer;
using MyNet.Utilities;

namespace MyClub.Plugins.Teamup.Import.File.Converters
{
    public class CsvPositionsConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var positions = text.OrEmpty().Split("|");
            var result = new List<RatedPositionImportDto>();

            foreach (var item in positions)
            {
                var values = item.Split(",");
                var position = values.Length >= 1 ? values[0] : string.Empty;

                if (string.IsNullOrEmpty(position)) continue;

                var rating = values.Length >= 2 ? values[1] : string.Empty;
                var isNatural = values.Length >= 3 && bool.Parse(values[2]) || rating == PositionRating.Natural.ToString();

                result.Add(new RatedPositionImportDto { Position = position, IsNatural = isNatural, Rating = rating });
            }

            return result;
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is IEnumerable<RatedPositionImportDto> positions
                ? positions.Select(x => new string?[] { x.Position?.Humanize(), x.Rating?.ToString() }.Humanize(",")).Humanize("|")
                : string.Empty;
    }
}
