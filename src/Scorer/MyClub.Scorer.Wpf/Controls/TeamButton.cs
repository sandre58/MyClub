// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class TeamButton : ItemButton<TeamViewModel>
    {
        static TeamButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(TeamButton), new FrameworkPropertyMetadata(typeof(TeamButton)));

        public TeamButton() { }

        #region ShowShortName

        public static readonly DependencyProperty ShowShortNameProperty = DependencyProperty.Register(nameof(ShowShortName), typeof(bool), typeof(TeamButton), new PropertyMetadata(false));

        public bool ShowShortName
        {
            get => (bool)GetValue(ShowShortNameProperty);
            set => SetValue(ShowShortNameProperty, value);
        }

        #endregion
    }
}
