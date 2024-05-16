// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Wpf.Views.TrainingPage.StatisticsTab
{
    /// <summary>
    /// Interaction logic for PlayerPageTrainingToolTipView.xaml
    /// </summary>
    public partial class TrainingStatisticsDetailsToolTipView : IChartTooltip
    {
        public TrainingStatisticsDetailsToolTipView()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string? DateLabel
        {
            get => (string?)GetValue(DateLabelProperty);
            set => SetValue(DateLabelProperty, value);
        }

        public static readonly DependencyProperty DateLabelProperty =
            DependencyProperty.Register(nameof(DateLabel), typeof(string), typeof(TrainingStatisticsDetailsToolTipView), new PropertyMetadata(string.Empty));

        public string? PlayerLabel
        {
            get => (string?)GetValue(PlayerLabelProperty);
            set => SetValue(PlayerLabelProperty, value);
        }

        public static readonly DependencyProperty PlayerLabelProperty =
            DependencyProperty.Register(nameof(PlayerLabel), typeof(string), typeof(TrainingStatisticsDetailsToolTipView), new PropertyMetadata(string.Empty));

        internal double? Rating
        {
            get => (double?)GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }

        public static readonly DependencyProperty RatingProperty =
            DependencyProperty.Register(nameof(Rating), typeof(double), typeof(TrainingStatisticsDetailsToolTipView), new PropertyMetadata(null));

        internal Attendance? Attendance
        {
            get => (Attendance?)GetValue(AttendanceProperty);
            set => SetValue(AttendanceProperty, value);
        }

        public static readonly DependencyProperty AttendanceProperty =
            DependencyProperty.Register(nameof(Attendance), typeof(Attendance), typeof(TrainingStatisticsDetailsToolTipView), new PropertyMetadata(null));

        private TooltipData? _data;

        public TooltipData Data
        {
            get => _data ?? new TooltipData();
            set
            {
                if (value is null)
                {
                    return;
                }

                _data = value;

                if (Data.Points[0].ChartPoint.ChartView is CartesianChart chart)
                {
                    var trainingLabels = chart.AxisX[0].Labels;
                    var playerLabels = chart.AxisY[0].Labels;

                    if (Data.Points[0].ChartPoint.Instance is HeatPoint point && trainingLabels is not null && point.X >= 0 && point.X < trainingLabels.Count && playerLabels is not null && point.Y >= 0 && point.Y < playerLabels.Count)
                    {
                        var hasRating = Data.SenderSeries.Equals(chart.Series[0]);
                        DateLabel = trainingLabels[(int)point.X];
                        PlayerLabel = playerLabels[(int)point.Y];

                        if (hasRating)
                        {
                            Rating = point.Weight;
                            Attendance = Domain.Enums.Attendance.Present;
                        }
                        else
                        {
                            Rating = -1;
                            Attendance = (Attendance?)point.Weight;

                        }
                    }
                }

                OnPropertyChanged(nameof(Data));
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
