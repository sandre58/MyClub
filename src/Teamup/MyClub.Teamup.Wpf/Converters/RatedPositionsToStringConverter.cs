// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MyNet.Humanizer;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Converters
{
    internal sealed class RatedPositionsToStringConverter : IValueConverter
    {
        private enum Result
        {
            BestCodes,

            NaturalCodes,

            GoodCodes,

        }

        private readonly Result _result;

        public static RatedPositionsToStringConverter NaturalCodes => new(Result.NaturalCodes);

        public static RatedPositionsToStringConverter GoodCodes => new(Result.GoodCodes);

        public static RatedPositionsToStringConverter BestCodes => new(Result.BestCodes);

        private RatedPositionsToStringConverter(Result result) => _result = result;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable<RatedPositionViewModel> positions) return string.Empty;

            var separator = parameter is not null ? parameter.ToString() : " / ";

            IEnumerable<RatedPositionViewModel>? result = null;
            switch (_result)
            {
                case Result.BestCodes:
                    result = positions.Where(x => (int)x.Rating >= (int)PositionRating.Natural - 2);
                    break;

                case Result.NaturalCodes:
                    result = positions.Where(x => x.IsNatural);
                    break;

                case Result.GoodCodes:
                    result = positions.Where(x => (int)x.Rating >= (int)PositionRating.Natural - 2 && !x.IsNatural);
                    break;

                default:
                    break;
            }

            return result is not null ? string.Join(separator, result.Select(x => x.Position.Humanize(true))) : string.Empty;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
