// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace MyClub.Scorer.Wpf.Views.HomePage.DashboardContent
{
    public partial class OverviewMatchesView
    {
        public OverviewMatchesView() => InitializeComponent();

        public static readonly DependencyProperty GhostTemplateProperty
            = DependencyProperty.RegisterAttached(nameof(GhostTemplate), typeof(DataTemplate), typeof(OverviewMatchesView), new PropertyMetadata(null));

        public DataTemplate GhostTemplate
        {
            get => (DataTemplate)GetValue(GhostTemplateProperty);
            set => SetValue(GhostTemplateProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty
            = DependencyProperty.RegisterAttached(nameof(ItemTemplate), typeof(DataTemplate), typeof(OverviewMatchesView), new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
    }
}
