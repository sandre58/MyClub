// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Wpf.Parameters;

namespace MyClub.Scorer.Wpf.Views.RankingPage
{
    public partial class RankingListSmallView
    {
        public RankingListSmallView() => InitializeComponent();

        public static readonly DependencyProperty ShowProgressionProperty
             = DependencyProperty.RegisterAttached(nameof(ShowProgression), typeof(bool), typeof(RankingListSmallView), new PropertyMetadata(false, OnShowProgressionChangedCallback));

        public bool ShowProgression
        {
            get => (bool)GetValue(ShowProgressionProperty);
            set => SetValue(ShowProgressionProperty, value);
        }

        private static void OnShowProgressionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListView listView) return;

            var columns = ListViewAssist.GetColumnLayouts(listView);

            var column = columns?.FirstOrDefault(x => x.Identifier == nameof(RankingRowViewModel.Progression));

            if (column is null) return;

            column.IsVisible = (bool)e.NewValue;
        }
    }
}
