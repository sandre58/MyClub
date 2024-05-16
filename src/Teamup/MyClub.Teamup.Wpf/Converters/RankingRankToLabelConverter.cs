// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.Converters
{
    internal sealed class RankingRankToLabelConverter : IMultiValueConverter
    {
        private enum Result
        {
            Background,

            Name,

            ShortName,

            Description
        }

        private readonly Result _result;

        public static RankingRankToLabelConverter Background { get; } = new(Result.Background);

        public static RankingRankToLabelConverter Name { get; } = new(Result.Name);

        public static RankingRankToLabelConverter ShortName { get; } = new(Result.ShortName);

        public static RankingRankToLabelConverter Description { get; } = new(Result.Description);

        private RankingRankToLabelConverter(Result result) => _result = result;

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Binding.DoNothing;
            if (!int.TryParse(values[0]?.ToString(), out var rank)) return Binding.DoNothing;

            var rankingRules = (RankingRules)values[1];

            var rankLabel = rankingRules?.Labels.FirstOrDefault(x => x.Key.IsValid(rank)).Value;

            return _result switch
            {
                Result.Background => rankLabel is not null && !string.IsNullOrEmpty(rankLabel.Color)
                                        ? new SolidColorBrush(rankLabel.Color.ToColor() ?? default)
                                        : Brushes.Transparent,
                Result.Name => rankLabel is not null
                                        ? rankLabel.Name
                                        : string.Empty,
                Result.ShortName => rankLabel is not null
                                        ? rankLabel.ShortName
                                        : string.Empty,
                Result.Description => rankLabel is not null
                                        ? rankLabel.Description ?? rankLabel.Name
                                        : string.Empty,
                _ => null,
            };
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
