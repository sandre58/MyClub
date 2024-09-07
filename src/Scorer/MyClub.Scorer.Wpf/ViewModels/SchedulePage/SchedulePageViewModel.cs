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
using MyNet.UI.ViewModels.Display;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class SchedulePageViewModel : PageViewModel
    {
        public SchedulePageViewModel(CompetitionInfoProvider competitionInfoProvider,
                                     MatchdaysProvider matchdaysProvider,
                                     MatchesProvider matchesProvider,
                                     TeamsProvider teamsProvider,
                                     StadiumsProvider stadiumsProvider,
                                     CompetitionCommandsService competitionCommandsService,
                                     MatchPresentationService matchPresentationService,
                                     AvailibilityCheckingService availibilityCheckingService)
        {
            competitionInfoProvider.WhenCompetitionChanged(x => MatchesPlanningViewModel = x switch
            {
                LeagueViewModel league => new(league.SchedulingParameters,
                                              matchesProvider,
                                              new ObservableSourceProvider<IMatchParent>(matchdaysProvider.Connect().Transform(x => (IMatchParent)x)),
                                              teamsProvider,
                                              stadiumsProvider,
                                              matchPresentationService,
                                              competitionCommandsService,
                                              availibilityCheckingService),
                _ => throw new NotImplementedException()
            },
            _ =>
            {
                MatchesPlanningViewModel?.Dispose();
                MatchesPlanningViewModel = null;
            });

            matchdaysProvider.WhenLoaded(() => MatchesPlanningViewModel?.Filters.Reset());
        }

        public MatchesPlanningViewModel? MatchesPlanningViewModel { get; private set; }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            base.LoadParameters(parameters);

            if (parameters?.Get<string>(NavigationCommandsService.DisplayModeParameterKey) is string displayMode && !string.IsNullOrEmpty(displayMode))
            {
                switch (displayMode)
                {
                    case nameof(DisplayModeByDate):
                    case nameof(DisplayModeByParent):
                    case nameof(DisplayModeDay):
                    case nameof(DisplayModeList):
                        MatchesPlanningViewModel?.Load(displayMode, parameters?.Get<object>(NavigationCommandsService.FilterParameterKey));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
