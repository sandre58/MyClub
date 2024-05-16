// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyClub.Teamup.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Converters
{
    public class CsvPositionsConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var positions = text.OrEmpty().Split("|");
            var result = new List<RatedPositionDto>();

            foreach (var item in positions)
            {
                var values = item.Split(",");
                var position = values.Length >= 1 ? values[0] : string.Empty;

                if (string.IsNullOrEmpty(position)) continue;

                var ratingStr = values.Length >= 2 ? values[1] : string.Empty;
                _ = int.TryParse(ratingStr, out var rating);
                var isNatural = values.Length >= 3 && bool.Parse(values[2]) || rating == (int)PositionRating.Natural;

                result.Add(new RatedPositionDto { Position = position.DehumanizeTo<Position>(), IsNatural = isNatural, Rating = (PositionRating)rating });
            }

            return result;
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is IEnumerable<RatedPositionDto> positions
                ? positions.Select(x => new string?[] { x.Position?.Humanize(), ((int)x.Rating).ToString() }.Humanize(",")).Humanize("|")
                : string.Empty;
    }
}
