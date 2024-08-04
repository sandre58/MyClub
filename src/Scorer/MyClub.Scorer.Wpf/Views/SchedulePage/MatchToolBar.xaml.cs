// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.Views.SchedulePage
{
    public partial class MatchToolBar
    {
        public MatchToolBar() => InitializeComponent();

        public static readonly DependencyProperty RescheduleConflictsCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RescheduleConflictsCommand), typeof(ICommand), typeof(MatchToolBar), new PropertyMetadata(null));

        public ICommand RescheduleConflictsCommand
        {
            get => (ICommand)GetValue(RescheduleConflictsCommandProperty);
            set => SetValue(RescheduleConflictsCommandProperty, value);
        }

        public static readonly DependencyProperty SelectConflictsCommandProperty
            = DependencyProperty.RegisterAttached(nameof(SelectConflictsCommand), typeof(ICommand), typeof(MatchToolBar), new PropertyMetadata(null));

        public ICommand SelectConflictsCommand
        {
            get => (ICommand)GetValue(SelectConflictsCommandProperty);
            set => SetValue(SelectConflictsCommandProperty, value);
        }
    }
}
