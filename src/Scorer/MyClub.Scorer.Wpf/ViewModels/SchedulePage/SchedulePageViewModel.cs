// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DynamicData;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Navigation.Models;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class SchedulePageViewModel : PageViewModel
    {
        public SchedulePageViewModel(ProjectInfoProvider projectInfoProvider,
                                     CompetitionInfoProvider competitionInfoProvider,
                                     MatchdaysProvider matchdaysProvider,
                                     MatchesProvider matchesProvider,
                                     TeamsProvider teamsProvider,
                                     StadiumsProvider stadiumsProvider,
                                     MatchPresentationService matchPresentationService,
                                     AvailibilityCheckingService availibilityCheckingService)
        {
            projectInfoProvider.WhenProjectClosing(() =>
            {
                MatchesPlanningViewModel?.Dispose();
                MatchesPlanningViewModel = null;
            });
            projectInfoProvider.WhenProjectLoaded(_ => MatchesPlanningViewModel = new(competitionInfoProvider.GetCompetition<LeagueViewModel>().SchedulingParameters,
                                               matchesProvider,
                                               new ObservableSourceProvider<IMatchParent>(matchdaysProvider.Connect().Transform(x => (IMatchParent)x)),
                                               teamsProvider,
                                               stadiumsProvider,
                                               matchPresentationService,
                                               availibilityCheckingService));
        }

        public MatchesPlanningViewModel? MatchesPlanningViewModel { get; private set; }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            base.LoadParameters(parameters);

            if (parameters?.Get<object>(NavigationCommandsService.SelectionParameterKey) is IEnumerable<Guid> seletedGuids)
                MatchesPlanningViewModel?.SelectItems(seletedGuids);
        }
    }
}
