// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.Converters
{
    internal class DateIsInHolidaysConverter : IValueConverter
    {
        private readonly HolidaysProvider _holidaysProvider;

        public static DateIsInHolidaysConverter? Default { get; private set; }

        public static void Initialize(HolidaysProvider holidaysProvider) => Default = new(holidaysProvider);

        private DateIsInHolidaysConverter(HolidaysProvider holidaysProvider) => _holidaysProvider = holidaysProvider;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is DateTime date && _holidaysProvider.Items.Any(x => x.ContainsDate(date));

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
