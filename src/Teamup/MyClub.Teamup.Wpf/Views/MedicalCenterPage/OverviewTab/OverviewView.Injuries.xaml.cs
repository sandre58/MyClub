// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace MyClub.Teamup.Wpf.Views.MedicalCenterPage.OverviewTab
{
    /// <summary>
    /// Interaction logic for TrainingStatisticsView.xaml
    /// </summary>
    public partial class OverviewInjuriesView
    {
        public OverviewInjuriesView() => InitializeComponent();

        #region IsScrollable

        public static readonly DependencyProperty IsScrollableProperty =
            DependencyProperty.Register(
                nameof(IsScrollable),
                typeof(bool),
                typeof(OverviewInjuriesView));

        public bool IsScrollable
        {
            get => (bool)GetValue(IsScrollableProperty);
            set => SetValue(IsScrollableProperty, value);
        }

        #endregion IsScrollable
    }
}
