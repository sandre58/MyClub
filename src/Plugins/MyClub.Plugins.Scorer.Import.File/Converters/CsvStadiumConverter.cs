// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Plugins.Scorer.Import.File.Converters
{
    public class CsvStadiumConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var result = text?.Split("|");

            if (result is null) return null;

            var name = result.Length > 0 ? result[0] : null;
            var ground = result.Length > 1 ? result[1] : default;
            var street = result.Length > 2 ? result[2] : null;
            var postalCode = result.Length > 3 ? result[3] : null;
            var city = result.Length > 4 ? result[4] : null;
            var country = result.Length > 5 ? result[5] : null;
            var latitude = result.Length > 6 && double.TryParse(result[6], out var v1) ? (double?)v1 : null;
            var longitude = result.Length > 7 && double.TryParse(result[7], out var v2) ? (double?)v2 : null;

            return new StadiumImportDto
            {
                Name = name,
                Ground = ground,
                Street = street,
                PostalCode = postalCode,
                City = city,
                Country = country,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
            => value is StadiumImportDto e ? string.Join("|", e.Name, e.Ground, e.Street, e.PostalCode, e.City, e.Country, e.Latitude, e.Longitude) : string.Empty;
    }
}
