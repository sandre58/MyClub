// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class MatchButton : ItemButton<MatchViewModel>
    {
        static MatchButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MatchButton), new FrameworkPropertyMetadata(typeof(MatchButton)));

        public MatchButton() { }

        #region ShowTime

        public static readonly DependencyProperty ShowTimeProperty = DependencyProperty.Register(nameof(ShowTime), typeof(bool), typeof(MatchButton), new PropertyMetadata(false));

        public bool ShowTime
        {
            get => (bool)GetValue(ShowTimeProperty);
            set => SetValue(ShowTimeProperty, value);
        }

        #endregion
    }
}
