// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Wpf.Converters
{
    internal sealed class MinuteToCanvasPositionConverter() : IMultiValueConverter
    {
        public static MinuteToCanvasPositionConverter Default = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var minute = values.Length > 0 ? (int)values[0] : 0;
            var matchFormat = values.Length > 1 && values[1] is MatchFormat format ? format : null;
            var useExtraTime = values.Length > 2 && bool.TryParse(values[2]?.ToString(), out var value) && value;

            return matchFormat is null ? Binding.DoNothing : minute * 100 / (useExtraTime ? matchFormat.GetFullTime(false).TotalMinutes : matchFormat.RegulationTime.GetFullTime(false).TotalMinutes);
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
