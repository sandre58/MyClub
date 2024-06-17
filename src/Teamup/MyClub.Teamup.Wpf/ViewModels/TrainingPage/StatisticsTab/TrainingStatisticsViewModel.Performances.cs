// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MyNet.UI.ViewModels.List;
using MyNet.Wpf.Converters;
using MyNet.Wpf.Extensions;
using MyNet.Wpf.Helpers;
using MyNet.Wpf.Schedulers;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Observable;
using MyNet.Observable.Translatables;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Helpers;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.StatisticsTab
{
    internal class TrainingStatisticsPerformancesViewModel : ListViewModel<PerformancesPlayerSerieWrapper>
    {
        private readonly IList<DisplayWrapper<DateTime>> _displayDates = [];
        private readonly LineSeries _averageSerie;

        public List<string>? AxeXLabels { get; private set; }

        public SectionsCollection HolidaysSections { get; private set; } = [];

        public SeriesCollection Series { get; private set; } = [];

        public bool ShowHolidaysSections { get; set; } = true;

        public bool ShowAverageSerie { get; set; } = true;

        public bool ShowAverageSection { get; set; } = true;

        public double AverageRating { get; private set; }

        public TrainingStatisticsPerformancesViewModel(ProjectInfoProvider projectInfoProvider, ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> playerTrainingStatistics, HolidaysProvider holidaysProvider)
            : base(source: playerTrainingStatistics.ToObservableChangeSet().ObserveOn(MyNet.UI.Threading.Scheduler.GetUIOrCurrent()).Transform(x => new PerformancesPlayerSerieWrapper(x, new SolidColorBrush(RandomGenerator.Color().ToColor().GetValueOrDefault()))).DisposeMany(),
                   parametersProvider: new TrainingStatisticsListParametersProvider(projectInfoProvider))
        {
            var mapper = Mappers.Xy<TrainingSessionViewModel>().X((session, index) => index).Y(session => session.Attendances.Any(x => x.Rating.HasValue) ? Math.Round(session.Attendances.Where(x => x.Rating.HasValue).Average(x => x.Rating!.Value), 2) : double.NaN);
            _averageSerie = new LineSeries(mapper)
            {
                Values = new ChartValues<TrainingSessionViewModel>(),
                Fill = Brushes.Transparent,
                LineSmoothness = 1,
                StrokeThickness = 2,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 6,
                DataLabels = true,
                DataLabelsTemplate = WpfHelper.GetResource<DataTemplate>("Teamup.DataTemplates.ChartPoint.TrainingSession"),
                Title = MyClubResources.Average
            };
            _ = _averageSerie.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(ShowAverageSerie)) { Source = this, Converter = BooleanToVisibilityConverter.CollapsedIfFalse });
            Series.Add(_averageSerie);

            Disposables.AddRange(
            [
                holidaysProvider.Connect().Subscribe(_ => RefreshHolidays(_displayDates.Select(x => x.Item), holidaysProvider.Items)),
                Source.ToObservableChangeSet().OnItemAdded(x => Series.Add(x.Serie)).Subscribe(),
                Source.ToObservableChangeSet().OnItemRemoved(x => Series.Remove(x.Serie)).Subscribe(),
                Items.ToObservableChangeSet().OnItemRemoved(x => x.IsVisible = false).Subscribe()
            ]);
        }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions, IEnumerable<HolidaysViewModel> holidays)
        {
            var performedSessions = sessions.Where(x => x.IsPerformed).OrderBy(x => x.StartDate).ToList();
            var dates = performedSessions.Select(x => x.StartDate.Date).ToList();

            AverageRating = Items.Any(x => !double.IsNaN(x.Item.Ratings.Average)) ? Items.Where(x => !double.IsNaN(x.Item.Ratings.Average)).Average(x => x.Item.Ratings.Average) : 0;

            RefreshAxeX(dates);
            RefreshSeries(performedSessions);
            RefreshHolidays(dates, holidays);
        }

        private void RefreshSeries(IEnumerable<TrainingSessionViewModel> sessions)
            => WpfScheduler.Current.Schedule(() =>
            {
                _averageSerie.Values = new ChartValues<TrainingSessionViewModel>(sessions);
                Items.ForEach(x => x.Serie.Values = new ChartValues<TrainingSessionViewModel>(sessions));

            });

        private void RefreshHolidays(IEnumerable<DateTime> dates, IEnumerable<HolidaysViewModel> holidays)
            => MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() =>
                {
                    var sections = UiHelper.HolidaysToSections(dates, holidays).Select(x =>
                    {
                        var section = new AxisSection
                        {
                            SectionOffset = x.start,
                            SectionWidth = x.width,
                            Fill = new SolidColorBrush(x.color)
                        };
                        section.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(ShowHolidaysSections))
                        {
                            Source = this,
                            Converter = BooleanToVisibilityConverter.CollapsedIfFalse
                        });

                        return section;
                    });
                    HolidaysSections.Set(sections);
                });

        private void RefreshAxeX(IEnumerable<DateTime> dates)
        {
            _displayDates.Set(dates.Select(x => new DisplayWrapper<DateTime>(x, new TranslatableObject<string>(() => x.ToString(MyClubResources.ChartDateFormat, CultureInfo.CurrentCulture)))));
            RefreshLabels();
        }

        private void RefreshLabels() => AxeXLabels = _displayDates.Select(x => x.DisplayName.Value.OrEmpty()).ToList();

        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            RefreshLabels();
        }
    }

    internal class PerformancesPlayerSerieWrapper : Wrapper<PlayerTrainingStatisticsViewModel>
    {
        public bool IsVisible { get; set; }

        public LineSeries Serie { get; }

        public PlayerViewModel Player => Item.Player;

        public PerformancesPlayerSerieWrapper(PlayerTrainingStatisticsViewModel item, Brush brush) : base(item)
        {
            Serie = new LineSeries(GetMapper(item))
            {
                Values = new ChartValues<TrainingSessionViewModel>(),
                Fill = Brushes.Transparent,
                Stroke = brush,
                LineSmoothness = 1,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 6,
                Title = item.Player.InverseName
            };
            _ = Serie.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(IsVisible)) { Source = this, Converter = BooleanToVisibilityConverter.CollapsedIfFalse });
        }

        private static CartesianMapper<TrainingSessionViewModel> GetMapper(PlayerTrainingStatisticsViewModel statistic)
            => Mappers.Xy<TrainingSessionViewModel>().X((session, index) => index).Y(session => session.Attendances.FirstOrDefault(x => x.Player == statistic.Player && x.Rating.HasValue)?.Rating ?? double.NaN);
    }
}
