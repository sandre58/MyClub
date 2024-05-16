// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace MyClub.Scorer.Wpf.Views.SchedulePage
{
    public partial class MatchesPlanningView
    {
        public MatchesPlanningView() => InitializeComponent();

        public static readonly DependencyProperty ShowPagingProperty
            = DependencyProperty.RegisterAttached(nameof(ShowPaging), typeof(bool), typeof(MatchesPlanningView), new PropertyMetadata(false));

        public bool ShowPaging
        {
            get => (bool)GetValue(ShowPagingProperty);
            set => SetValue(ShowPagingProperty, value);
        }


        public static readonly DependencyProperty ShowGroupingProperty
            = DependencyProperty.RegisterAttached(nameof(ShowGrouping), typeof(bool), typeof(MatchesPlanningView), new PropertyMetadata(true));

        public bool ShowGrouping
        {
            get => (bool)GetValue(ShowGroupingProperty);
            set => SetValue(ShowGroupingProperty, value);
        }
    }
}
