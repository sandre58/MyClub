// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab
{
    internal class OverviewViewModel : NavigableWorkspaceViewModel
    {
        private readonly MainItemsProvider _mainItemsProvider;

        public OverviewCountViewModel CountViewModel { get; }

        public OverviewTrainingSessionsViewModel SessionsViewModel { get; }

        public OverviewAttendanceViewModel AttendanceViewModel { get; }

        public OverviewAttendancesViewModel AttendancesViewModel { get; }

        public OverviewPerformancesViewModel PerformancesViewModel { get; }

        public OverviewFettleViewModel FettleViewModel { get; }

        public OverviewInjuriesViewModel MedicalCenterViewModel { get; }

        public int CountSessions { get; set; } = 2;

        public int CountPlayerAttendancesStatistics { get; set; } = 3;

        public int CountPlayerRatingsStatistics { get; set; } = 1;

        public int CountPlayerLastRatingsStatistics { get; set; } = 4;

        public ICommand NavigateToMedicalCenterCommand { get; }

        public OverviewViewModel(
            MainItemsProvider squadsProvider,
            TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
        {
            _mainItemsProvider = squadsProvider;

            CountViewModel = new OverviewCountViewModel();
            SessionsViewModel = new OverviewTrainingSessionsViewModel(CountSessions);
            AttendanceViewModel = new OverviewAttendanceViewModel();
            AttendancesViewModel = new OverviewAttendancesViewModel(CountPlayerAttendancesStatistics);
            PerformancesViewModel = new OverviewPerformancesViewModel(CountPlayerRatingsStatistics);
            FettleViewModel = new OverviewFettleViewModel(CountPlayerLastRatingsStatistics);
            MedicalCenterViewModel = new OverviewInjuriesViewModel();

            NavigateToMedicalCenterCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToMedicalCenterPage(MedicalCenterPageTab.Overview));

            Disposables.AddRange(
            [
                squadsProvider.ConnectPlayers().AutoRefresh(x => x.IsInjured).Subscribe(_ => MedicalCenterViewModel.Refresh(squadsProvider.Players.Select(x => x.Injury).NotNull()))
            ]);

            trainingStatisticsRefreshDeferrer.Subscribe(this, RefreshStatistics);

            Messenger.Default.Register<MainTeamChangedMessage>(this, _ => RefreshStatistics());
        }

        private void RefreshStatistics()
        {
            try
            {
                LogManager.Trace($"Refresh - Trainings Overview");

                var sessions = _mainItemsProvider.TrainingSessions.ToList();
                var players = _mainItemsProvider.Players.ToList();
                var playerTrainingStatistics = players.Select(x => x.TrainingStatistics).ToList();
                var currentInjuries = players.Select(x => x.Injury).NotNull().ToList();

                CountViewModel.Refresh(sessions);
                SessionsViewModel.Refresh(sessions);
                AttendanceViewModel.Refresh(sessions);
                AttendancesViewModel.Refresh(playerTrainingStatistics);
                PerformancesViewModel.Refresh(playerTrainingStatistics);
                FettleViewModel.Refresh(playerTrainingStatistics);
                MedicalCenterViewModel.Refresh(currentInjuries);
            }
            catch (TaskCanceledException)
            {
                LogManager.Debug("Computing training statistics has been cancelled.");
            }
        }

        protected override string CreateTitle() => MyClubResources.Overview;
    }
}
