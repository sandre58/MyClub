// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Helpers;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Translatables;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Wpf.LiveCharts;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPageTrainingViewModel : SubItemViewModel<PlayerViewModel>
    {
        private readonly HolidaysProvider _holidaysProvider;

        public IDisplayViewModel Display { get; }

        public ChartSerie<TrainingAttendanceViewModel> RatingsSerie { get; set; }

        public ChartSerie<TrainingAttendanceViewModel> NoRatingsSerie { get; set; }

        public ChartSerie<TrainingAttendanceViewModel> AbsencesSerie { get; set; }

        public List<string>? AxeXLabels { get; private set; }

        public SectionsCollection HolidaysSections { get; private set; } = [];

        public PlayerPageTrainingViewModel(HolidaysProvider holidaysProvider, TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
        {
            _holidaysProvider = holidaysProvider;
            Display = new DisplayViewModel().AddMode<DisplayModeChart>(true).AddMode<DisplayModeList>();

            RatingsSerie = new ChartSerie<TrainingAttendanceViewModel>(
                Mappers.Xy<TrainingAttendanceViewModel>()
                .X(x => Item?.TrainingStatistics?.PerformedAttendances.OrderBy(y => y.Session.StartDate).ToList().IndexOf(x) ?? 0)
                .Y(x => x.Rating!.Value)
                .Fill(x => UiHelper.GetBrushFromRating(x.Rating!.Value))
                .Stroke(x => UiHelper.GetBrushFromRating(x.Rating!.Value))
                );

            AbsencesSerie = new ChartSerie<TrainingAttendanceViewModel>(
                Mappers.Xy<TrainingAttendanceViewModel>()
                .X(x => Item?.TrainingStatistics?.PerformedAttendances.OrderBy(y => y.Session.StartDate).ToList().IndexOf(x) ?? 0)
                .Y(x => 0)
                .Fill(x => UiHelper.GetBrushFromAttendance(x.Attendance))
                .Stroke(x => UiHelper.GetBrushFromAttendance(x.Attendance))
                );

            NoRatingsSerie = new ChartSerie<TrainingAttendanceViewModel>(
                Mappers.Xy<TrainingAttendanceViewModel>()
                .X(x => Item?.TrainingStatistics?.PerformedAttendances.OrderBy(y => y.Session.StartDate).ToList().IndexOf(x) ?? 0)
                .Y(x => 0)
                .Fill(x => UiHelper.GetBrushFromAttendance(x.Attendance))
                .Stroke(x => UiHelper.GetBrushFromAttendance(x.Attendance))
                );

            trainingStatisticsRefreshDeferrer.Subscribe(RefreshAll);
            Disposables.Add(_holidaysProvider.Connect().SubscribeAll(RefreshHolidays));
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (Item is null) return;

            RefreshAxeX();
            RefreshSeries();
            RefreshHolidays();
        }

        private void RefreshSeries()
        {
            if (Item is null) return;

            LogManager.Trace($"Refresh - Player Details:{Item} [Trainings Series]");

            var ratingAttendances = Item.TrainingStatistics.PerformedAttendances.Where(x => x.Rating.HasValue).OrderBy(x => x.Session.StartDate).ToList();
            var notRatingAttendances = Item.TrainingStatistics.PerformedAttendances.Where(x => !x.Rating.HasValue && (x.Attendance == Attendance.Present || x.Attendance == Attendance.Unknown)).OrderBy(x => x.Session.StartDate).ToList();
            var absentAttendances = Item.TrainingStatistics.PerformedAttendances.Where(x => x.Attendance != Attendance.Present && x.Attendance != Attendance.Unknown).OrderBy(x => x.Session.StartDate).ToList();

            RatingsSerie.Update(ratingAttendances);
            NoRatingsSerie.Update(notRatingAttendances);
            AbsencesSerie.Update(absentAttendances);
        }

        private void RefreshHolidays()
        {
            if (Item is null) return;

            LogManager.Trace($"Refresh - Player Details:{Item} [Holidays Sections]");

            var dates = Item.TrainingStatistics.PerformedAttendances.Select(x => x.Session.StartDate).OrderBy(x => x);

            MyNet.Observable.Threading.Scheduler.GetUIOrCurrent().Schedule(() => HolidaysSections.Set(UiHelper.HolidaysToSections(dates, _holidaysProvider.Items).Select(x => new AxisSection
            {
                SectionOffset = x.start,
                SectionWidth = x.width,
                Fill = new SolidColorBrush(x.color)
            })));
        }

        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            RefreshAxeX();
        }

        private void RefreshAxeX()
        {
            if (Item is null) return;

            LogManager.Trace($"Refresh - Player Details:{Item} [Trainings Labels]");

            AxeXLabels = Item.TrainingStatistics.PerformedAttendances.OrderBy(x => x.Session.StartDate)
                                                                     .Select(x => new DisplayWrapper<DateTime>(x.Session.StartDate, new TranslatableObject<string>(() => x.Session.StartDate.ToString(MyClubResources.ChartDateFormat, CultureInfo.CurrentCulture))))
                                                                     .Select(x => x.DisplayName.Value.OrEmpty())
                                                                     .ToList();
        }
    }
}
