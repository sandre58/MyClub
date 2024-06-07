// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DynamicData;
using MyNet.UI.Navigation.Models;
using MyNet.Observable.Collections.Providers;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Application.Services;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class SchedulePageViewModel : PageViewModel
    {
        public SchedulePageViewModel(ProjectInfoProvider projectInfoProvider,
                                     MatchdaysProvider matchdaysProvider,
                                     MatchesProvider matchesProvider,
                                     TeamsProvider teamsProvider,
                                     StadiumsProvider stadiumsProvider,
                                     MatchPresentationService matchPresentationService,
                                     AvailibilityCheckingService availibilityCheckingService)
        {
            MatchesPlanningViewModel = new(matchesProvider,
                                           new ObservableSourceProvider<IMatchParent>(matchdaysProvider.Connect().Transform(x => (IMatchParent)x)),
                                           teamsProvider,
                                           stadiumsProvider,
                                           matchPresentationService,
                                           availibilityCheckingService);

            projectInfoProvider.WhenProjectLoaded(_ => MatchesPlanningViewModel.Reset());
        }

        public MatchesPlanningViewModel MatchesPlanningViewModel { get; }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            base.LoadParameters(parameters);

            if (parameters?.Get<object>(NavigationCommandsService.SelectionParameterKey) is IEnumerable<Guid> seletedGuids)
                MatchesPlanningViewModel.SelectItems(seletedGuids);
        }
    }
}
