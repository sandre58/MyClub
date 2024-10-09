// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.HomePage;
using MyClub.Scorer.Wpf.ViewModels.PastPositionsPage;
using MyClub.Scorer.Wpf.ViewModels.RankingPage;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyClub.Scorer.Wpf.ViewModels.StatisticsPage;
using MyNet.UI.Navigation;
using MyNet.Utilities.Suspending;
using MyNet.Wpf.Schedulers;

namespace MyClub.Scorer.Wpf.Services
{
    internal sealed class NavigationCommandsService : WorkspaceNavigationService, IDisposable
    {
        public const string SelectionParameterKey = "Selection";
        public const string DisplayModeParameterKey = "DisplayMode";
        public const string FilterParameterKey = "Filter";

        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly CompositeDisposable _disposables = [];
        private readonly Suspender _navigationSuspender = new();

        public NavigationCommandsService(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;
            projectInfoProvider.UnloadRunner.RegisterOnStart(this, () =>
            {
                using (_navigationSuspender.Suspend())
                {
                    if (CurrentContext?.Page is not HomePageViewModel)
                        WpfScheduler.Current.Schedule(() => NavigateToHomePage());
                    ClearJournal();
                }
            });
        }

        public static void NavigateToHomePage() => NavigationManager.NavigateTo<HomePageViewModel>();

        public static void NavigateToSchedulePage(string? displayMode = null, object? filterValue = null)
        {
            var parameters = new List<KeyValuePair<string, object?>>();

            if (displayMode is not null) parameters.Add(new KeyValuePair<string, object?>(DisplayModeParameterKey, displayMode));
            if (filterValue is not null) parameters.Add(new KeyValuePair<string, object?>(FilterParameterKey, filterValue));

            NavigationManager.NavigateTo<SchedulePageViewModel>(parameters);
        }

        public static void NavigateToPastPositionsPage(IEnumerable<Guid>? selectedIds = null)
        {
            if (selectedIds is not null)
                NavigationManager.NavigateTo<PastPositionsPageViewModel>(selectedIds, SelectionParameterKey);
            else
                NavigationManager.NavigateTo<PastPositionsPageViewModel>();
        }

        public static void NavigateToStatisticsPage() => NavigationManager.NavigateTo<StatisticsPageViewModel>();

        public static void NavigateToRankingPage() => NavigationManager.NavigateTo<RankingPageViewModel>();
        public void Dispose()
        {
            _projectInfoProvider.UnloadRunner.Unregister(this);
            _disposables.Dispose();
        }
    }
}
