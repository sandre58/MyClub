// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.PastPositionsPage;

namespace MyClub.Scorer.Wpf.Views.PastPositionsPage
{
    /// <summary>
    /// Interaction logic for PlayerPageTrainingToolTipView.xaml
    /// </summary>
    public partial class PastPositionsToolTipView : IChartTooltip
    {
        public PastPositionsToolTipView()
        {
            InitializeComponent();

            DataContext = this;
        }

        internal MatchViewModel? Match
        {
            get => (MatchViewModel)GetValue(MatchProperty);
            set => SetValue(MatchProperty, value);
        }

        public static readonly DependencyProperty MatchProperty =
            DependencyProperty.Register(nameof(Match), typeof(MatchViewModel), typeof(PastPositionsToolTipView), new PropertyMetadata(null));

        private TooltipData? _data;

        public TooltipData Data
        {
            get => _data ?? new TooltipData();
            set
            {
                _data = value;

                if (value is null)
                    return;

                var data = value.Points[0].ChartPoint.Instance as PastPosition;
                Match = data?.Match;

                OnPropertyChanged(nameof(Data));
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
