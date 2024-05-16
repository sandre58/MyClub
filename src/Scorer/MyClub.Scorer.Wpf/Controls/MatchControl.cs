// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class MatchControl : Control
    {
        static MatchControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MatchControl), new FrameworkPropertyMetadata(typeof(MatchControl)));

        public static readonly DependencyProperty MatchProperty = DependencyProperty.Register(nameof(Match), typeof(MatchViewModel), typeof(MatchControl));
        public static readonly DependencyProperty EnableContextMenuProperty = DependencyProperty.Register(nameof(EnableContextMenu), typeof(bool), typeof(MatchControl), new FrameworkPropertyMetadata(true));

        public MatchViewModel Match
        {
            get => (MatchViewModel)GetValue(MatchProperty);
            set => SetValue(MatchProperty, value);
        }

        public bool EnableContextMenu
        {
            get => (bool)GetValue(EnableContextMenuProperty);
            set => SetValue(EnableContextMenuProperty, value);
        }
    }
}
