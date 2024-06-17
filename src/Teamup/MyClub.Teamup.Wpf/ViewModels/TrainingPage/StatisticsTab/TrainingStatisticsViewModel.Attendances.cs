// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Media;
using DynamicData.Aggregation;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Helpers;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Translatables;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Wpf.Converters;
using MyNet.Wpf.LiveCharts;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.StatisticsTab
{
    internal class TrainingStatisticsAttendancesViewModel : NavigableWorkspaceViewModel
    {
        private readonly IList<DisplayWrapper<DateTime>> _displayDates = [];

        public List<string>? AxeXLabels { get; private set; }

        public SectionsCollection HolidaysSections { get; private set; } = [];

        public ChartSerie<TrainingSessionViewModel> PresentsSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> AbsentsSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> ApologySerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> InjuredSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> InHolidaysSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> InSelectionSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> RestingSerie { get; private set; }

        public ChartSerie<TrainingSessionViewModel> UnknownSerie { get; private set; }

        public bool ShowHolidaysSections { get; set; } = true;

        public bool ShowLabels { get; set; } = true;

        public bool ShowInPercent { get; set; }

        public int CountPlayers { get; private set; }

        public double AverageAttendances { get; set; }

        public double AveragePresents { get; set; }

        public double AverageAbsents { get; set; }

        public double AverageApologized { get; set; }

        public double AverageInjured { get; set; }

        public double AverageInHolidays { get; set; }

        public double AverageInSelection { get; set; }

        public double AverageResting { get; set; }

        public double AverageUnknown { get; set; }

        public TrainingStatisticsAttendancesViewModel(HolidaysProvider holidaysProvider)
        {
            PresentsSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Present));
            AbsentsSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Absent));
            ApologySerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Apology));
            InjuredSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Injured));
            InHolidaysSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.InHolidays));
            InSelectionSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.InSelection));
            RestingSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Resting));
            UnknownSerie = new ChartSerie<TrainingSessionViewModel>(GetMapper(Attendance.Unknown), string.Empty, false);

            Disposables.Add(holidaysProvider.Connect().Subscribe(_ => RefreshHolidays(_displayDates.Select(x => x.Item), holidaysProvider.Items)));
        }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions, IEnumerable<HolidaysViewModel> holidays, int countPlayers)
        {
            var performedSessions = sessions.Where(x => x.IsPerformed).OrderBy(x => x.StartDate).ToList();
            var dates = performedSessions.Select(x => x.StartDate.Date).ToList();

            RefreshAxeX(dates);
            RefreshSeries(performedSessions);
            RefreshHolidays(dates, holidays);

            CountPlayers = countPlayers;

            AverageAttendances = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count) : 0;
            AveragePresents = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Present)) : 0;
            AverageAbsents = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Absent)) : 0;
            AverageApologized = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Apology)) : 0;
            AverageInjured = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Injured)) : 0;
            AverageInHolidays = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.InHolidays)) : 0;
            AverageInSelection = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.InSelection)) : 0;
            AverageResting = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Resting)) : 0;
            AverageUnknown = performedSessions.Count != 0 ? performedSessions.Average(x => x.Attendances.Count(x => x.Attendance == Attendance.Unknown)) : 0;
        }

        private static CartesianMapper<TrainingSessionViewModel> GetMapper(Attendance attendance)
            => Mappers.Xy<TrainingSessionViewModel>().X((session, index) => index).Y(session => session.Attendances.Count(x => x.Attendance == attendance));

        private void RefreshSeries(IList<TrainingSessionViewModel> sessions)
        {
            PresentsSerie.Update(sessions);
            AbsentsSerie.Update(sessions);
            ApologySerie.Update(sessions);
            InjuredSerie.Update(sessions);
            InHolidaysSerie.Update(sessions);
            InSelectionSerie.Update(sessions);
            RestingSerie.Update(sessions);
            UnknownSerie.Update(sessions);
        }

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
                    section.SetBinding(System.Windows.UIElement.VisibilityProperty, new Binding(nameof(ShowHolidaysSections))
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
}
