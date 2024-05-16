// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Views.PlayerPage
{
    /// <summary>
    /// Interaction logic for PlayerPageTrainingToolTipView.xaml
    /// </summary>
    public partial class PlayerPageTrainingChartToolTipView : IChartTooltip
    {
        public PlayerPageTrainingChartToolTipView()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string DateLabel
        {
            get => (string)GetValue(DateLabelProperty);
            set => SetValue(DateLabelProperty, value);
        }

        public static readonly DependencyProperty DateLabelProperty =
            DependencyProperty.Register(nameof(DateLabel), typeof(string), typeof(PlayerPageTrainingChartToolTipView), new PropertyMetadata(string.Empty));

        internal TrainingAttendanceViewModel? Attendance
        {
            get => (TrainingAttendanceViewModel)GetValue(AttendanceProperty);
            set => SetValue(AttendanceProperty, value);
        }

        public static readonly DependencyProperty AttendanceProperty =
            DependencyProperty.Register(nameof(Attendance), typeof(TrainingAttendanceViewModel), typeof(PlayerPageTrainingChartToolTipView), new PropertyMetadata(null));

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

                Attendance = Data?.Points[0].ChartPoint.Instance as TrainingAttendanceViewModel;
                DateLabel = _data.XFormatter.Invoke(_data.Points[0].ChartPoint.X);
                OnPropertyChanged(nameof(Data));
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
