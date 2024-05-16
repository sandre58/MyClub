// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class PlayerGauge : Control
    {
        static PlayerGauge() => DefaultStyleKeyProperty.OverrideMetadata(typeof(PlayerGauge), new FrameworkPropertyMetadata(typeof(PlayerGauge)));

        #region Orientation

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(PlayerGauge), new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Player

        public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(nameof(Player), typeof(PlayerViewModel), typeof(PlayerGauge), new PropertyMetadata(null));

        public PlayerViewModel Player
        {
            get => (PlayerViewModel)GetValue(PlayerProperty);
            set => SetValue(PlayerProperty, value);
        }

        #endregion

        #region Tab

        public static readonly DependencyProperty TabProperty = DependencyProperty.Register(nameof(Tab), typeof(PlayerPageTab), typeof(PlayerGauge), new PropertyMetadata(PlayerPageTab.Overview));

        public PlayerPageTab Tab
        {
            get => (PlayerPageTab)GetValue(TabProperty);
            set => SetValue(TabProperty, value);
        }

        #endregion

        #region Value

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(PlayerGauge), new PropertyMetadata(0.0));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        #endregion

        #region From

        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(nameof(From), typeof(double), typeof(PlayerGauge), new PropertyMetadata(0.0));

        public double From
        {
            get => (double)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        #endregion

        #region To

        public static readonly DependencyProperty ToProperty = DependencyProperty.Register(nameof(To), typeof(double), typeof(PlayerGauge), new PropertyMetadata(100.0));

        public double To
        {
            get => (double)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        #endregion
    }
}
