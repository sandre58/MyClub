// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class VirtualTeamButton : ItemButton<IVirtualTeamViewModel>
    {
        static VirtualTeamButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualTeamButton), new FrameworkPropertyMetadata(typeof(VirtualTeamButton)));

        public VirtualTeamButton() { }

        #region ShowShortName

        public static readonly DependencyProperty ShowShortNameProperty = DependencyProperty.Register(nameof(ShowShortName), typeof(bool), typeof(VirtualTeamButton), new PropertyMetadata(false));

        public bool ShowShortName
        {
            get => (bool)GetValue(ShowShortNameProperty);
            set => SetValue(ShowShortNameProperty, value);
        }

        #endregion
    }
}
