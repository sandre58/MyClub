// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Wpf.Converters
{
    internal sealed class PositionToLayoutConverter : IMultiValueConverter
    {
        private enum LayoutProperty
        {
            Left,

            Top,

            Width,

            Height
        }

        private readonly LayoutProperty _layout;

        public static PositionToLayoutConverter Height { get; } = new(LayoutProperty.Height);

        public static PositionToLayoutConverter Width { get; } = new(LayoutProperty.Width);

        public static PositionToLayoutConverter Left { get; } = new(LayoutProperty.Left);

        public static PositionToLayoutConverter Top { get; } = new(LayoutProperty.Top);

        private PositionToLayoutConverter(LayoutProperty layout) => _layout = layout;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return Binding.DoNothing;

            var width = (double)values[0];
            var height = (double)values[1];
            var position = (Position)values[2];
            var offsetX = values.Length >= 4 ? (int?)values[3] : 0;
            var offsetY = values.Length >= 5 ? (int?)values[4] : 0;

            var column = (int)position.Side;
            var row = (int)position.Line;
            var rowInverse = Position.RowsCount - 1 - row;

            var coef = 0.5 / Position.RowsCount;
            var decrementWidthByRow = width * coef;
            var rowWidth = width - decrementWidthByRow * rowInverse;
            var itemWidth = rowWidth / Position.ColumnsCount;
            var itemHeight = height / Position.RowsCount;
            var itemLeft = (width - rowWidth) / 2 + itemWidth * column + (offsetX ?? 0) * width / 100;
            var itemTop = row * itemHeight + (offsetY ?? 0) * height / 100;

            return _layout switch
            {
                LayoutProperty.Left => itemLeft,
                LayoutProperty.Top => itemTop,
                LayoutProperty.Width => itemWidth,
                LayoutProperty.Height => itemHeight,
                _ => Binding.DoNothing,
            };
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
