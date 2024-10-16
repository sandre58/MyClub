// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class TeamControl : Control
    {
        static TeamControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(TeamControl), new FrameworkPropertyMetadata(typeof(TeamControl)));

        public static readonly DependencyProperty TeamProperty = DependencyProperty.Register(nameof(Team), typeof(IVirtualTeamViewModel), typeof(TeamControl));

        public IVirtualTeamViewModel Team
        {
            get => (IVirtualTeamViewModel)GetValue(TeamProperty);
            set => SetValue(TeamProperty, value);
        }

        public static readonly DependencyProperty IsAwayProperty = DependencyProperty.Register(nameof(IsAway), typeof(bool), typeof(TeamControl), new PropertyMetadata(false));

        public bool IsAway
        {
            get => (bool)GetValue(IsAwayProperty);
            set => SetValue(IsAwayProperty, value);
        }

        public static readonly DependencyProperty ShowShortNameProperty = DependencyProperty.Register(nameof(ShowShortName), typeof(bool), typeof(TeamControl), new PropertyMetadata(false));

        public bool ShowShortName
        {
            get => (bool)GetValue(ShowShortNameProperty);
            set => SetValue(ShowShortNameProperty, value);
        }

        public static readonly DependencyProperty QualificationStateProperty = DependencyProperty.Register(nameof(QualificationState), typeof(QualificationState), typeof(TeamControl), new PropertyMetadata(QualificationState.Unknown));

        public QualificationState QualificationState
        {
            get => (QualificationState)GetValue(QualificationStateProperty);
            set => SetValue(QualificationStateProperty, value);
        }

        public static readonly DependencyProperty LogoSizeProperty = DependencyProperty.Register(nameof(LogoSize), typeof(double), typeof(TeamControl), new PropertyMetadata(24.0d));

        public double LogoSize
        {
            get => (double)GetValue(LogoSizeProperty);
            set => SetValue(LogoSizeProperty, value);
        }

        #region TextWrapping

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(TeamControl), new PropertyMetadata(TextWrapping.NoWrap));

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        #endregion
    }
}
