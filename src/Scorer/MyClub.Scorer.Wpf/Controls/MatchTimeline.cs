// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Controls
{
    [TemplatePart(Name = PeriodsItemsControlName, Type = typeof(Button))]
    internal class MatchTimeline : ItemsControl
    {
        private const string PeriodsItemsControlName = "PART_Periods";
        private ItemsControl? _periodsItemsControl;
        static MatchTimeline() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MatchTimeline), new FrameworkPropertyMetadata(typeof(MatchTimeline)));

        #region MatchFormat

        public static readonly DependencyProperty MatchFormatProperty = DependencyProperty.Register(nameof(MatchFormat), typeof(MatchFormat), typeof(MatchTimeline), new PropertyMetadata(MatchFormat.Default, OnMatchFormatChanged));

        public MatchFormat MatchFormat
        {
            get => (MatchFormat)GetValue(MatchFormatProperty);
            set => SetValue(MatchFormatProperty, value);
        }

        private static void OnMatchFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as MatchTimeline)?.OnMatchFormatChanged();

        private void OnMatchFormatChanged() => UpdatePeriods();

        private void UpdatePeriods()
        {
            if (_periodsItemsControl is null) return;

            var extraTimePeriods = ShowExtraTime ? MatchFormat?.ExtraTime?.Number.Range().SelectMany(x => new List<int> { (int)MatchFormat.ExtraTime.Duration.TotalMinutes * (x - 1), (int)MatchFormat.ExtraTime.Duration.TotalMinutes * x }) : null;
            var periods = MatchFormat?.RegulationTime.Number.Range()
                                                            .SelectMany(x => new List<int> { (int)MatchFormat.RegulationTime.Duration.TotalMinutes * (x - 1), (int)MatchFormat.RegulationTime.Duration.TotalMinutes * x })
                                                            .Union(extraTimePeriods?.Select(x => x + (int)MatchFormat.RegulationTime.GetFullTime(false).TotalMinutes) ?? [])
                                                            .Distinct()
                                                            .ToList();
            _periodsItemsControl.ItemsSource = periods;
            SetValue(TotalMinutesPropertyKey, periods?.MaxOrDefault());
        }
        #endregion

        #region ShowExtraTime

        public static readonly DependencyProperty ShowExtraTimeProperty = DependencyProperty.Register(nameof(ShowExtraTime), typeof(bool), typeof(MatchTimeline), new PropertyMetadata(true, OnShowExtraTimeChanged));

        public bool ShowExtraTime
        {
            get => (bool)GetValue(ShowExtraTimeProperty);
            set => SetValue(ShowExtraTimeProperty, value);
        }

        private static void OnShowExtraTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as MatchTimeline)?.OnShowExtraTimeChanged();

        private void OnShowExtraTimeChanged() => UpdatePeriods();

        #endregion

        #region BarWidth

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(nameof(BarWidth), typeof(double), typeof(MatchTimeline), new PropertyMetadata(10.0D));

        public double BarWidth
        {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        #endregion

        #region PeriodSize

        public static readonly DependencyProperty PeriodSizeProperty = DependencyProperty.Register(nameof(PeriodSize), typeof(double), typeof(MatchTimeline), new PropertyMetadata(20.0D));

        public double PeriodSize
        {
            get => (double)GetValue(PeriodSizeProperty);
            set => SetValue(PeriodSizeProperty, value);
        }

        #endregion

        #region MinuteSize

        public static readonly DependencyProperty MinuteSizeProperty = DependencyProperty.Register(nameof(MinuteSize), typeof(double), typeof(MatchTimeline), new PropertyMetadata(18.0D));

        public double MinuteSize
        {
            get => (double)GetValue(MinuteSizeProperty);
            set => SetValue(MinuteSizeProperty, value);
        }

        #endregion

        internal static readonly DependencyPropertyKey TotalMinutesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(TotalMinutes),
        typeof(int),
        typeof(MatchTimeline),
        new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty TotalMinutesProperty = TotalMinutesPropertyKey.DependencyProperty;

        public int TotalMinutes => (int)GetValue(TotalMinutesProperty);

        public override void OnApplyTemplate()
        {
            _periodsItemsControl = GetTemplateChild(PeriodsItemsControlName) as ItemsControl;

            UpdatePeriods();
        }
    }
}
