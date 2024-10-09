// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyNet.Wpf.Controls;

namespace MyClub.Scorer.Wpf.Controls
{
    public class ItemDropDownButton<T> : DropDownButton
    {
        protected ItemDropDownButton() { }

        #region Orientation

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ItemDropDownButton<T>), new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Item

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(T), typeof(ItemDropDownButton<T>));

        public T Item
        {
            get => (T)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        #endregion
    }
}
