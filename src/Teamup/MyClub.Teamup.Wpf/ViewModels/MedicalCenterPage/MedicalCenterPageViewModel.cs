// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.InjuriesTab;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.StatisticsTab;
using MyNet.Observable.Collections.Extensions;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage
{
    internal class MedicalCenterPageViewModel : PageViewModel
    {
        public OverviewViewModel OverviewViewModel { get; }

        public InjuriesViewModel InjuriesViewModel { get; }

        public InjuriesStatisticsPlayersViewModel StatisticsPlayerViewModel { get; }

        public InjuriesStatisticsInjuriesViewModel StatisticsInjuryTypeViewModel { get; }

        public MedicalCenterPageViewModel(ProjectInfoProvider projectInfoProvider,
                                          MainItemsProvider mainItemsProvider,
                                          InjuriesProvider injuriesProvider,
                                          PlayerPresentationService playerPresentationService,
                                          InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer) : base(projectInfoProvider)
        {
            OverviewViewModel = new OverviewViewModel(mainItemsProvider, injuriesStatisticsRefreshDeferrer);
            InjuriesViewModel = new InjuriesViewModel(injuriesProvider, playerPresentationService);

            Disposables.Add(InjuriesViewModel.Items.ToObservableChangeSet(x => x.Id).GroupOnProperty(x => x.Player).Transform(x => x.Key).Bind(out var players).Subscribe());

            StatisticsPlayerViewModel = new InjuriesStatisticsPlayersViewModel(players, Observable.FromEventPattern<FilteredEventArgs>(x => InjuriesViewModel.Filtered += x, x => InjuriesViewModel.Filtered -= x).Select(x => new Func<InjuryViewModel, bool>(y => x.EventArgs.Filters.ToList().Match(y))));
            StatisticsInjuryTypeViewModel = new InjuriesStatisticsInjuriesViewModel(InjuriesViewModel.Items);
            AddSubWorkspaces([OverviewViewModel, InjuriesViewModel, StatisticsPlayerViewModel, StatisticsInjuryTypeViewModel]);
        }

        protected override void ResetFromMainTeams(IEnumerable<Guid>? mainTeams)
        {
            base.ResetFromMainTeams(mainTeams);
            InjuriesViewModel.ResetFiltersWithTeams(mainTeams, $"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.TeamId)}");
        }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            // DisplayMode
            if (parameters.Get<MedicalCenterDisplayMode?>(NavigationCommandsService.DisplayModeParameterKey, null) is MedicalCenterDisplayMode displayMode)
            {
                GoToTab(InjuriesViewModel);
                switch (displayMode)
                {
                    case MedicalCenterDisplayMode.Grid:
                        InjuriesViewModel.Display.SetMode<DisplayModeGrid>();
                        break;
                    case MedicalCenterDisplayMode.List:
                        InjuriesViewModel.Display.SetMode<DisplayModeList>();
                        break;
                }
            }

            // Tab
            else
                base.LoadParameters(parameters);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            InjuriesViewModel.Dispose();
            OverviewViewModel.Dispose();
            StatisticsInjuryTypeViewModel.Dispose();
            StatisticsPlayerViewModel.Dispose();
        }
    }
}
