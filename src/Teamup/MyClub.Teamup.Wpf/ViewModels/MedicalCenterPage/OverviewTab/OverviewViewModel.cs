// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Application.Deferrers;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab
{
    internal class OverviewViewModel : NavigableWorkspaceViewModel
    {
        private readonly MainItemsProvider _mainItemsProvider;

        public OverviewCountViewModel CountViewModel { get; }

        public OverviewInjuriesViewModel CurrentInjuriesViewModel { get; }

        public OverviewInjuriesViewModel RecentInjuriesViewModel { get; }

        public OverviewFettleViewModel FettleViewModel { get; }

        public int CountPlayerUnaivalables { get; set; } = 4;

        public OverviewViewModel(
            MainItemsProvider mainItemsProvider,
            InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer)
        {
            _mainItemsProvider = mainItemsProvider;

            CountViewModel = new OverviewCountViewModel();
            CurrentInjuriesViewModel = new OverviewInjuriesViewModel();
            RecentInjuriesViewModel = new OverviewInjuriesViewModel();
            FettleViewModel = new OverviewFettleViewModel(CountPlayerUnaivalables);

            injuriesStatisticsRefreshDeferrer.Subscribe(RefreshStatistics);
            Messenger.Default.Register<MainTeamChangedMessage>(this, _ => RefreshStatistics());
        }

        private void RefreshStatistics()
        {
            try
            {
                LogManager.Trace($"Refresh - Injuries Overview");

                var injuries = _mainItemsProvider.Players.SelectMany(x => x.Injuries);

                CountViewModel.Refresh(injuries.ToList());
                CurrentInjuriesViewModel.Refresh(injuries.Where(x => x.IsCurrent).ToList());
                RecentInjuriesViewModel.Refresh(injuries.Where(x => x.EndDate.HasValue && !x.IsCurrent && x.EndDate.Value.IsAfter(DateTime.Now.AddDays(-14))).ToList());
                FettleViewModel.Refresh(_mainItemsProvider.Players.Select(x => x.InjuryStatistics).ToList());
            }
            catch (TaskCanceledException)
            {
                LogManager.Debug("Computing injuries statistics has been cancelled.");
            }
        }

        protected override string CreateTitle() => MyClubResources.Overview;

        protected override void Cleanup()
        {
            base.Cleanup();
            CountViewModel.Dispose();
            CurrentInjuriesViewModel.Dispose();
            RecentInjuriesViewModel.Dispose();
            FettleViewModel.Dispose();
        }
    }
}
