// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MyNet.Wpf.Helpers;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Converters
{
    internal sealed class TeamOfMatchConverter : IMultiValueConverter
    {
        private enum Result
        {
            Brush,
        }

        private readonly Result _result;

        public static TeamOfMatchConverter Brush { get; } = new(Result.Brush);

        private TeamOfMatchConverter(Result result) => _result = result;

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Binding.DoNothing;

            var match = (MatchViewModel)values[0];
            var team = (TeamViewModel)values[1];

            var result = match.GetResult(team);


            switch (_result)
            {
                case Result.Brush:
                    switch (result)
                    {
                        case MatchResult.None:
                            return WpfHelper.GetResource<SolidColorBrush>("MyNet.Brushes.None");
                        case MatchResult.Won:
                            return WpfHelper.GetResource<SolidColorBrush>("MyNet.Brushes.Positive");
                        case MatchResult.Drawn:
                            return WpfHelper.GetResource<SolidColorBrush>("MyNet.Brushes.Warning");
                        case MatchResult.Lost:
                            return WpfHelper.GetResource<SolidColorBrush>("MyNet.Brushes.Negative");
                    }
                    break;
            }

            return null;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
