// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Application.Converters
{
    public class CsvHalfFormatConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var result = text?.Split(",");

            if (result is null) return null;

            var number = result.Length > 0 && int.TryParse(result[0], out var v1) ? v1 : 2;
            var duration = result.Length > 1 && TimeSpan.TryParse(result[1], CultureInfo.CurrentCulture, out var v2) ? v2 : TimeSpan.FromMinutes(45);

            return new HalfFormat(number, duration);
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
            => value is HalfFormat e ? string.Join(",", e.Number, e.Duration) : string.Empty;
    }
}
