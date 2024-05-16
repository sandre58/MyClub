// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Application.Converters
{
    public class CsvStadiumConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var result = text?.Split("|");

            if (result is null) return null;

            var name = result.Length > 0 ? result[0] : null;
            var ground = result.Length > 1 && Enum.TryParse<Ground>(result[1], out var value) ? value : default;
            var street = result.Length > 2 ? result[2] : null;
            var postalCode = result.Length > 3 ? result[3] : null;
            var city = result.Length > 4 ? result[4] : null;
            var country = result.Length > 5 && Country.TryFromName(result[5], out var val) ? val : null;
            var latitude = result.Length > 6 && double.TryParse(result[6], out var v1) ? (double?)v1 : null;
            var longitude = result.Length > 7 && double.TryParse(result[7], out var v2) ? (double?)v2 : null;

            return new StadiumDto
            {
                Name = name,
                Ground = ground,
                Address = !string.IsNullOrEmpty(street)
                          || !string.IsNullOrEmpty(postalCode)
                          || !string.IsNullOrEmpty(city)
                          || country is not null
                          || latitude is not null
                          || longitude is not null
                          ? new Address(street, postalCode, city, country, latitude, longitude)
                          : null
            };
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
            => value is StadiumDto e ? string.Join("|", e.Name, e.Ground.ToString(), e.Address?.Street, e.Address?.PostalCode, e.Address?.City, e.Address?.Country, e.Address?.Latitude, e.Address?.Longitude) : string.Empty;
    }
}
