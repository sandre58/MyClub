// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Converters
{
    public sealed class DateToPeriodColorConverter(DateToPeriodColorConverter.ReturnType returnType) : IMultiValueConverter
    {
        public enum ReturnType
        {
            Label,

            Brush
        }

        private readonly ReturnType _returnType = returnType;

        public static DateToPeriodColorConverter Label { get; } = new(ReturnType.Label);

        public static DateToPeriodColorConverter Brush { get; } = new(ReturnType.Brush);

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not DateTime date || values[1] is not IEnumerable<ISchedulingPeriodViewModel> periods) return Binding.DoNothing;

            var period = periods.LastOrDefault(x => date.IsBetween(x.StartDate, x.EndDate));
            var hasOpacity = double.TryParse(parameter?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var opacity);
            return period is null
                ? Binding.DoNothing
                : _returnType == ReturnType.Label ? period.Label : new SolidColorBrush(period.Color) { Opacity = hasOpacity ? opacity : 1 };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
