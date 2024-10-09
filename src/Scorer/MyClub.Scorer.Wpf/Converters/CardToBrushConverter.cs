// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MyClub.Domain.Enums;
using MyNet.Wpf.Helpers;

namespace MyClub.Scorer.Wpf.Converters
{
    internal class CardToBrushConverter
        : IValueConverter
    {
        public static readonly CardToBrushConverter Default = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not CardColor cardColor ? Binding.DoNothing : WpfHelper.GetResource<Brush>($"Scorer.Brushes.Card.{cardColor}");

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
