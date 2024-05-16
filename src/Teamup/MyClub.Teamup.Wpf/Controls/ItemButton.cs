// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace MyClub.Teamup.Wpf.Controls
{
    public class ItemButton<T> : Button
    {
        protected ItemButton() { }

        #region Orientation

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ItemButton<T>), new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Item

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(T), typeof(ItemButton<T>));

        public T Item
        {
            get => (T)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        #endregion
    }
}
