// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.SchedulingTab;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.StatisticsTab;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage
{
    internal class TrainingPageViewModel : PageViewModel
    {
        private readonly ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> _playerTrainingStatistics;
        private readonly HolidaysProvider _holidaysProvider;
        private readonly ProjectInfoProvider _projectInfoProvider;

        public OverviewViewModel OverviewViewModel { get; }

        public SchedulingViewModel PlanningViewModel { get; }

        public TrainingSessionsViewModel SessionsViewModel { get; }

        public TrainingStatisticsPlayersViewModel StatisticsPlayersViewModel { get; }

        public TrainingStatisticsAttendancesViewModel StatisticsAttendancesViewModel { get; }

        public TrainingStatisticsPerformancesViewModel StatisticsPerformancesViewModel { get; }

        public TrainingStatisticsDetailsViewModel StatisticsDetailsViewModel { get; }

        public TrainingPageViewModel(
            ProjectInfoProvider projectInfoProvider,
            MainItemsProvider teamsProvider,
            TrainingSessionsProvider trainingSessionsProvider,
            PlayersProvider playersProvider,
            HolidaysProvider holidaysProvider,
            CyclesProvider cyclesProvider,
            TrainingSessionPresentationService trainingSessionPresentationService,
            HolidaysPresentationService holidaysPresentationService,
            CyclePresentationService cyclePresentationService,
            TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer) : base(projectInfoProvider)
        {
            _holidaysProvider = holidaysProvider;
            _projectInfoProvider = projectInfoProvider;
            SessionsViewModel = new TrainingSessionsViewModel(trainingSessionsProvider, holidaysProvider, cyclesProvider, trainingSessionPresentationService);

            Disposables.AddRange(
[
                Observable.FromEventPattern<FilteredEventArgs>(x => SessionsViewModel.Filtered += x, x => SessionsViewModel.Filtered -= x).Subscribe(_ =>
                {
                    if(!trainingStatisticsRefreshDeferrer.IsDeferred())
                        RefreshStatistics();
                }),
                playersProvider.Connect().Transform(x => new PlayerTrainingStatisticsViewModel(x)).DisposeMany().Bind(out _playerTrainingStatistics).Subscribe()
            ]);

            OverviewViewModel = new OverviewViewModel(teamsProvider, trainingStatisticsRefreshDeferrer);
            PlanningViewModel = new SchedulingViewModel(holidaysProvider, cyclesProvider, holidaysPresentationService, cyclePresentationService);
            StatisticsPlayersViewModel = new TrainingStatisticsPlayersViewModel(projectInfoProvider, _playerTrainingStatistics);
            StatisticsPerformancesViewModel = new TrainingStatisticsPerformancesViewModel(projectInfoProvider, _playerTrainingStatistics, holidaysProvider);
            StatisticsAttendancesViewModel = new TrainingStatisticsAttendancesViewModel(holidaysProvider);
            StatisticsDetailsViewModel = new TrainingStatisticsDetailsViewModel(projectInfoProvider, _playerTrainingStatistics);

            AddSubWorkspaces([OverviewViewModel, PlanningViewModel, SessionsViewModel, StatisticsPlayersViewModel, StatisticsPerformancesViewModel, StatisticsAttendancesViewModel, StatisticsDetailsViewModel]);

            trainingStatisticsRefreshDeferrer.Subscribe(this, RefreshStatistics);
        }

        protected override void ResetFromMainTeams(IEnumerable<Guid>? mainTeams)
        {
            base.ResetFromMainTeams(mainTeams);

            SessionsViewModel.ResetFiltersWithTeams(mainTeams, $"{nameof(TrainingSessionViewModel.Teams)}.{nameof(TeamViewModel.Id)}");
            resetStatisticsFilters(StatisticsDetailsViewModel);
            resetStatisticsFilters(StatisticsPerformancesViewModel);
            resetStatisticsFilters(StatisticsPlayersViewModel);

            void resetStatisticsFilters(IListViewModel listViewModel)
            {
                if (listViewModel.Filters is TrainingStatisticsSpeedFiltersViewModel speedFilters)
                    listViewModel.ResetFiltersWithTeams(speedFilters.OnlyMyPlayers ? mainTeams : _projectInfoProvider.GetCurrentProject()?.Club.Teams.Select(x => x.Id).ToList(), $"{nameof(PlayerTrainingStatisticsViewModel.Player)}.{nameof(PlayerViewModel.TeamId)}");
            }
        }

        protected override void ResetFromProject(Project? project)
        {
            base.ResetFromProject(project);

            PlanningViewModel.StartDate = project?.Season.Period.Start ?? DateTime.Today.FirstDayOfYear();
            PlanningViewModel.EndDate = project?.Season.Period.End ?? DateTime.Today.LastDayOfYear();
        }

        private void RefreshStatistics()
        {
            try
            {
                LogManager.Trace($"Refresh - Trainings Statistics");

                _playerTrainingStatistics.ForEach(x => x.Refresh(SessionsViewModel.Items));
                StatisticsAttendancesViewModel.Refresh(SessionsViewModel.Items, _holidaysProvider.Items, _playerTrainingStatistics.Count);
                StatisticsPerformancesViewModel.Refresh(SessionsViewModel.Items, _holidaysProvider.Items);
                StatisticsDetailsViewModel.Refresh(SessionsViewModel.Items);
            }
            catch (TaskCanceledException)
            {
                LogManager.Debug("Computing training statistics has been cancelled.");
            }
        }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            // DisplayMode
            if (parameters.Get<TrainingSessionsDisplayMode?>(NavigationCommandsService.DisplayModeParameterKey, null) is TrainingSessionsDisplayMode displayMode)
            {
                GoToTab(SessionsViewModel);
                switch (displayMode)
                {
                    case TrainingSessionsDisplayMode.Calendar:
                        SessionsViewModel.Display.SetMode<DisplayModeCalendar>();
                        break;
                    case TrainingSessionsDisplayMode.List:
                        SessionsViewModel.Display.SetMode<DisplayModeList>();
                        break;
                }
            }

            // Tab
            else
                base.LoadParameters(parameters);
        }

        protected override void Cleanup()
        {
            PlanningViewModel.Dispose();
            SessionsViewModel.Dispose();
            OverviewViewModel.Dispose();
            StatisticsPlayersViewModel.Dispose();
            StatisticsAttendancesViewModel.Dispose();
            StatisticsPerformancesViewModel.Dispose();
            StatisticsDetailsViewModel.Dispose();
            base.Cleanup();
        }
    }
}
