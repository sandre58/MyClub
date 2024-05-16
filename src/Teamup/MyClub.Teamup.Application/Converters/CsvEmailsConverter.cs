// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MyClub.Teamup.Application.Dtos;
using MyNet.Humanizer;
using MyNet.Utilities;

namespace MyClub.Teamup.Application.Converters
{
    public class CsvEmailsConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            var contacts = text.OrEmpty().Split("|");
            var result = new List<EmailDto>();

            foreach (var contact in contacts)
            {
                var values = contact.Split(",");
                var isDefault = false;
                var value = values.Length >= 1 ? values[0] : string.Empty;

                if (string.IsNullOrEmpty(value)) continue;

                var label = values.Length >= 2 && !bool.TryParse(values[1], out isDefault) ? values[1] : string.Empty;
                isDefault = !isDefault && values.Length >= 3 && bool.Parse(values[2]);

                result.Add(new EmailDto { Label = label, Default = isDefault, Value = value });
            }

            return result;
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is IEnumerable<EmailDto> contacts
                ? contacts.Select(x => new string?[] { x.Value, x.Label }.Humanize(",")).Humanize("|")
                : string.Empty;
    }
}
