// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using DynamicData.Binding;
using LiveCharts;
using LiveCharts.Defaults;
using MyNet.UI.ViewModels.List;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.StatisticsTab
{
    internal class TrainingStatisticsDetailsViewModel : ListViewModel<PlayerTrainingStatisticsViewModel>
    {
        public ChartValues<HeatPoint> RatingsValues { get; private set; } = [];

        public ChartValues<HeatPoint> NotRatingsValues { get; private set; } = [];

        public IList<TrainingSessionViewModel> Sessions { get; private set; } = [];

        public List<string> AxesX { get; private set; } = [];

        public List<string> AxesY { get; private set; } = [];

        public TrainingStatisticsDetailsViewModel(ProjectInfoProvider projectInfoProvider, ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> playersStatistics)
            : base(source: playersStatistics.ToObservableChangeSet(),
                   parametersProvider: new TrainingStatisticsListParametersProvider(projectInfoProvider)) => Disposables.Add(Items.ToObservableChangeSet().Subscribe(_ => Refresh(Sessions)));

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions)
        {
            Sessions = sessions.Where(x => x.IsPerformed).ToList();
            var inversePlayersStatistics = Items.Reverse().ToList();

            RefreshAxeX();
            RefreshAxeY();

            RatingsValues = new ChartValues<HeatPoint>(Items.SelectMany(x => x.PerformedAttendances.Where(x => x.Rating.HasValue).Select(y => new HeatPoint(Sessions.IndexOf(y.Session), inversePlayersStatistics.IndexOf(x), y.Rating!.Value))));
            NotRatingsValues = new ChartValues<HeatPoint>(Items.SelectMany(x => x.PerformedAttendances.Where(x => !x.Rating.HasValue).Select(y => new HeatPoint(Sessions.IndexOf(y.Session), inversePlayersStatistics.IndexOf(x), (double)y.Attendance))));
        }

        private void RefreshAxeX() => AxesX = Sessions.Select(x => x.StartDate.ToString(MyClubResources.ChartDateFormat, CultureInfo.CurrentCulture)).ToList();

        private void RefreshAxeY()
        {
            var inversePlayersStatistics = Items.Reverse().ToList();
            AxesY = inversePlayersStatistics.Select(x => x.Player.FullName).ToList();
        }

        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            RefreshAxeX();
        }
    }
}
